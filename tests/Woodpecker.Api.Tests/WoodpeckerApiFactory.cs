using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Woodpecker.Infrastructure;

namespace Woodpecker.Api.Tests;

// Remplace la base PostgreSQL par une base EF Core InMemory (nom unique par instance de
// factory) : les tests d'intégration testent le pipeline HTTP complet (routing, auth,
// validation, MediatR, EF Core) sans dépendre d'un vrai serveur PostgreSQL.
public class WoodpeckerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Seed:Enabled", "false");
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WoodpeckerDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            services.AddDbContext<WoodpeckerDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
