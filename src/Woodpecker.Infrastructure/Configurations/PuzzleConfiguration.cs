using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Configurations;

public class PuzzleConfiguration : IEntityTypeConfiguration<Puzzle>
{
    public void Configure(EntityTypeBuilder<Puzzle> builder)
    {
        builder.ToTable("Puzzles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // Id généré côté domaine, jamais par la base

        builder.Property(p => p.Fen)
            .IsRequired();

        builder.Property(p => p.Rating)
            .IsRequired();

        // SolutionMoves (IReadOnlyList<string>) est stocké comme une seule colonne texte,
        // les coups séparés par un espace (même format que la séquence de coups UCI dans
        // la base de puzzles Lichess). Alternative "propre" : une table de coups séparée,
        // mais elle serait en lecture seule et toujours lue en bloc dans l'ordre -> inutile
        // ici, ça ajouterait une jointure sans bénéfice réel.
        builder.Property(p => p.SolutionMoves)
            .HasConversion(
                moves => string.Join(' ', moves),
                value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (a, b) => a!.SequenceEqual(b!),
                a => a.Aggregate(0, (hash, move) => HashCode.Combine(hash, move.GetHashCode())),
                a => a.ToList()));
    }
}
