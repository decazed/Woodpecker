using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Woodpecker.Api.Middleware;
using Woodpecker.Application;
using Woodpecker.Infrastructure;
using Woodpecker.Infrastructure.Auth;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Ajoute le bouton "Authorize" dans Swagger UI pour coller un token Bearer et tester
    // les endpoints protégés directement depuis la doc.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez le token JWT (sans le préfixe \"Bearer \").",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

// La clé n'est pas dans appsettings.json (seulement dans appsettings.Development.json ou en
// variable d'environnement Jwt__Key) : on échoue explicitement au démarrage plutôt que de
// laisser la lib JWT lever une erreur obscure à la première requête authentifiée.
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Configuration Jwt:Key absente ou trop courte (32 caractères minimum pour HMAC-SHA256). " +
        "Définir la variable d'environnement Jwt__Key ou compléter appsettings.Development.json.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Applique les migrations en attente au démarrage. Pratique pour le dev (docker compose up
// suffit, pas de commande manuelle à lancer) ; en production on préférerait généralement
// un job de migration séparé du démarrage de l'API. IsRelational() exclut le provider
// InMemory utilisé par les tests d'intégration, qui ne supporte pas Migrate().
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Woodpecker.Infrastructure.WoodpeckerDbContext>();
    if (dbContext.Database.IsRelational())
        dbContext.Database.Migrate();

    // Catalogue de puzzles initial (Seed:Enabled=false dans les tests d'intégration, qui
    // importent leurs propres puzzles et ne doivent pas dépendre du contenu du fichier).
    if (app.Configuration.GetValue("Seed:Enabled", true))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<Woodpecker.Infrastructure.Puzzles.PuzzleCatalogSeeder>();
        await seeder.SeedIfEmptyAsync(Path.Combine(AppContext.BaseDirectory, "Seed", "puzzles.csv"));
    }
}

// Le logging de requêtes doit englober le middleware d'exceptions : placé après, il verrait
// l'exception brute et loggerait un 500 alors que la réponse réelle est un 401/404/409.
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program; // exposé pour WebApplicationFactory dans les tests d'intégration
