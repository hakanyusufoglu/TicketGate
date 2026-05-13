using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Identity.Domain.Entities;

namespace TicketGate.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .IsRequired();
    }
}
