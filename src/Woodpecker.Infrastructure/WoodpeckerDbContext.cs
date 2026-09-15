using Microsoft.EntityFrameworkCore;
using Woodpecker.Application.Abstractions;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure;

public class WoodpeckerDbContext : DbContext, IApplicationDbContext
{
    public WoodpeckerDbContext(DbContextOptions<WoodpeckerDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Puzzle> Puzzles => Set<Puzzle>();
    public DbSet<PuzzleSet> PuzzleSets => Set<PuzzleSet>();
    public DbSet<TrainingCycle> TrainingCycles => Set<TrainingCycle>();
    public DbSet<PuzzleAttempt> PuzzleAttempts => Set<PuzzleAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WoodpeckerDbContext).Assembly);
    }
}
