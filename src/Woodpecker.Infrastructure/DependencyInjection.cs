using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Woodpecker.Application.Abstractions;
using Woodpecker.Infrastructure.Auth;
using Woodpecker.Infrastructure.Puzzles;

namespace Woodpecker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<WoodpeckerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("WoodpeckerDb")));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<WoodpeckerDbContext>());
        services.AddScoped<IMoveValidator, SolutionMoveValidator>();
        services.AddScoped<ILichessPuzzleCsvParser, LichessPuzzleCsvParser>();
        services.AddScoped<PuzzleCatalogSeeder>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
