using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Event.Domain.Entities;

namespace TicketGate.Event.Infrastructure.Persistence.Configurations;

public sealed class PerformerConfiguration : IEntityTypeConfiguration<Performer>
{
    public void Configure(EntityTypeBuilder<Performer> builder)
    {
        builder.ToTable("performers");

        builder.HasKey(performer => performer.Id);

        builder.Property(performer => performer.Id)
            .ValueGeneratedNever();

        builder.Property(performer => performer.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(performer => performer.Bio)
            .HasMaxLength(2000);
    }
}
