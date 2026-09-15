using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Woodpecker.Domain;

namespace Woodpecker.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever(); // Id généré côté domaine, jamais par la base

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320); // longueur max théorique d'un email (RFC 5321)

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>() // stocké en texte ("Member"/"Admin") plutôt qu'en entier, plus lisible en base
            .HasMaxLength(20);
    }
}
