using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Configurations;

public class PuzzleSetItemConfiguration : IEntityTypeConfiguration<PuzzleSetItem>
{
    public void Configure(EntityTypeBuilder<PuzzleSetItem> builder)
    {
        builder.ToTable("PuzzleSetItems");

        // Pas d'Id propre : clé composite (PuzzleSetId, PuzzleId), une ligne = un puzzle
        // dans un set donné. PuzzleSetId est une "shadow property" (FK ajoutée par la
        // relation HasMany côté PuzzleSetConfiguration, pas une propriété C# explicite).
        builder.Property<Guid>("PuzzleSetId");
        builder.HasKey("PuzzleSetId", nameof(PuzzleSetItem.PuzzleId));

        builder.Property(i => i.Position).IsRequired();

        // FK réelle vers le catalogue de puzzles : Restrict empêche de supprimer un
        // Puzzle encore référencé par un set existant (le catalogue est partagé/global,
        // on préfère un échec explicite à une suppression silencieuse qui casserait un set).
        builder.HasOne<Puzzle>()
            .WithMany()
            .HasForeignKey(i => i.PuzzleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.PuzzleId);
    }
}
