using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TicketGate.Event.Infrastructure.Persistence.Configurations;

public sealed class EventConfiguration : IEntityTypeConfiguration<Domain.Entities.Event>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(eventEntity => eventEntity.Id);

        builder.Property(eventEntity => eventEntity.Id)
            .ValueGeneratedNever();

        builder.Property(eventEntity => eventEntity.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.Description)
            .HasMaxLength(1000);

        builder.Property(eventEntity => eventEntity.StartsAt)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.EndsAt)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.IsPublished)
            .IsRequired();

        builder.Property(eventEntity => eventEntity.CreatedAt)
            .IsRequired();

        builder.HasOne(eventEntity => eventEntity.Venue)
            .WithMany()
            .HasForeignKey(eventEntity => eventEntity.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eventEntity => eventEntity.Performer)
            .WithMany()
            .HasForeignKey(eventEntity => eventEntity.PerformerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(eventEntity => eventEntity.StartsAt);

        builder.HasIndex(eventEntity => eventEntity.IsPublished);

        builder.HasIndex(eventEntity => eventEntity.VenueId);
    }
}
