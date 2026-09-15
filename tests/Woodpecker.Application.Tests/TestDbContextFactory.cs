using Microsoft.EntityFrameworkCore;
using Woodpecker.Infrastructure;

namespace Woodpecker.Application.Tests;

// Une base EF Core InMemory par test (nom de base unique via Guid) : nécessaire pour
// tester les handlers avec IApplicationDbContext (option A, cf. décision d'architecture),
// puisqu'on ne peut pas mocker proprement un DbSet<T>.
internal static class TestDbContextFactory
{
    public static WoodpeckerDbContext Create() => Create(Guid.NewGuid().ToString());

    // Même nom de base = même données, mais un tracker d'entités vierge : sert à simuler
    // deux requêtes HTTP successives (un scope/DbContext chacune) sur les mêmes données,
    // pour attraper les bugs de chargement (Include manquant) invisibles avec un seul contexte.
    public static WoodpeckerDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<WoodpeckerDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new WoodpeckerDbContext(options);
    }
}
