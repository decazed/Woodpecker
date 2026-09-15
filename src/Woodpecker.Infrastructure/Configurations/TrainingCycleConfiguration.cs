using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Configurations;

public class TrainingCycleConfiguration : IEntityTypeConfiguration<TrainingCycle>
{
    public void Configure(EntityTypeBuilder<TrainingCycle> builder)
    {
        builder.ToTable("TrainingCycles");

        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Id).ValueGeneratedNever(); // Id généré côté domaine, jamais par la base

        builder.Property(tc => tc.PuzzleSetId).IsRequired();
        builder.Property(tc => tc.UserId).IsRequired();
        builder.Property(tc => tc.CycleNumber).IsRequired();
        builder.Property(tc => tc.StartedAt).IsRequired();

        builder.HasIndex(tc => tc.PuzzleSetId);
        builder.HasIndex(tc => tc.UserId);

        // La propriété Attempts n'est qu'un accesseur de lecture pratique vers le champ
        // _attempts ; on la retire du modèle EF pour éviter un conflit de mapping (EF associe
        // sinon automatiquement _attempts à la fois à Attempts et à la relation ci-dessous),
        // et on mappe la relation directement sur le champ privé pour ne pas casser
        // l'encapsulation du domaine.
        builder.Ignore(tc => tc.Attempts);

        builder.HasMany<PuzzleAttempt>("_attempts")
            .WithOne()
            .HasForeignKey(pa => pa.TrainingCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation("_attempts")!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
