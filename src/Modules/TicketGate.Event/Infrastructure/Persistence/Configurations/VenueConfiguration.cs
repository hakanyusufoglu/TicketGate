using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Event.Domain.Entities;

namespace TicketGate.Event.Infrastructure.Persistence.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(venue => venue.Id);

        builder.Property(venue => venue.Id)
            .ValueGeneratedNever();

        builder.Property(venue => venue.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(venue => venue.Location)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(venue => venue.SeatMap)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
