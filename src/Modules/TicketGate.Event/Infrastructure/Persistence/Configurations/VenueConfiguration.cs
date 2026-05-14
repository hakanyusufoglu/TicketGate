using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Event.Domain.Entities;

namespace TicketGate.Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Venue entity EF Core konfigurasyonu. SeatMap jsonb olarak saklanir;
/// typed value object uygulama kodunda string JSON manipilasyonunu engeller.
/// </summary>
public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    /// <summary>
    /// Venue tablo kolonlarini ve SeatMap jsonb conversion ayarini tanimlar.
    /// Conversion PostgreSQL jsonb kolonu ile Core SeatMap contract'i arasinda calisir.
    /// </summary>
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
            .HasConversion(
                seatMap => JsonSerializer.Serialize(seatMap, JsonSerializerOptions.Default),
                value => JsonSerializer.Deserialize<TicketGate.Core.Domain.SeatMap>(
                    value,
                    JsonSerializerOptions.Default)!)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
