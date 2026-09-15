using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Configurations;

public class PuzzleSetConfiguration : IEntityTypeConfiguration<PuzzleSet>
{
    public void Configure(EntityTypeBuilder<PuzzleSet> builder)
    {
        builder.ToTable("PuzzleSets");

        builder.HasKey(ps => ps.Id);
        builder.Property(ps => ps.Id).ValueGeneratedNever(); // Id généré côté domaine, jamais par la base

        builder.Property(ps => ps.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ps => ps.OwnerId)
            .IsRequired();

        // Pas de FK réelle vers Users : PuzzleSet ne connaît son propriétaire que par Id
        // (frontière d'agrégat), cf. le choix déjà fait dans TrainingCycle/PuzzleAttempt.
        builder.HasIndex(ps => ps.OwnerId);

        // PuzzleIds n'est qu'un accesseur de lecture calculé à partir de _items ;
        // on le retire du modèle EF et on mappe la vraie relation sur le champ privé,
        // vers la table de jointure PuzzleSetItems (cf. PuzzleSetItemConfiguration).
        builder.Ignore(ps => ps.PuzzleIds);

        builder.HasMany<PuzzleSetItem>("_items")
            .WithOne()
            .HasForeignKey("PuzzleSetId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation("_items")!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
