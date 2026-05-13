using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Identity.Domain.Entities;

namespace TicketGate.Identity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.Id)
            .ValueGeneratedNever();

        builder.Property(refreshToken => refreshToken.Token)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.Token)
            .IsUnique();

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(refreshToken => refreshToken.ExpiresAt)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.ExpiresAt);

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .IsRequired();
    }
}
