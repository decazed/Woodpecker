using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Configurations;

public class PuzzleAttemptConfiguration : IEntityTypeConfiguration<PuzzleAttempt>
{
    public void Configure(EntityTypeBuilder<PuzzleAttempt> builder)
    {
        builder.ToTable("PuzzleAttempts");

        builder.HasKey(pa => pa.Id);

        // L'Id est toujours généré côté domaine (Guid.NewGuid() dans PuzzleAttempt.Create),
        // jamais par la base : sans ça, EF suppose par convention qu'un Guid non vide
        // correspond à une ligne déjà existante et tente un UPDATE au lieu d'un INSERT
        // quand l'entité est découverte via la collection _attempts de TrainingCycle.
        builder.Property(pa => pa.Id).ValueGeneratedNever();

        builder.Property(pa => pa.PuzzleId).IsRequired();
        builder.Property(pa => pa.IsSuccess).IsRequired();
        builder.Property(pa => pa.Duration).IsRequired(); // mappé nativement sur "interval" par Npgsql
        builder.Property(pa => pa.AttemptedAt).IsRequired();

        builder.HasIndex(pa => pa.PuzzleId);
    }
}
