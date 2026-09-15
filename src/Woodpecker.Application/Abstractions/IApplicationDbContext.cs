using Microsoft.EntityFrameworkCore;
using Woodpecker.Domain;

namespace Woodpecker.Application.Abstractions;

// Expose uniquement ce dont les handlers ont besoin, pas tout WoodpeckerDbContext
// (par exemple pas de OnModelCreating). Implémentée par WoodpeckerDbContext en Infrastructure.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Puzzle> Puzzles { get; }
    DbSet<PuzzleSet> PuzzleSets { get; }
    DbSet<TrainingCycle> TrainingCycles { get; }

    // Exposé séparément (en plus de TrainingCycle.Attempts, encapsulé) pour permettre
    // des requêtes d'agrégation directes (stats, cf. étape 8) sans charger chaque
    // TrainingCycle complet en mémoire.
    DbSet<PuzzleAttempt> PuzzleAttempts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
