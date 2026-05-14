using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Booking.Domain.Entities;

namespace TicketGate.Booking.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core ticket tablo konfigurasyonu. xmin ile optimistic concurrency, event_id ve status composite index'i ile musait koltuk sorgu performansi saglanir.
/// Status string olarak saklanir; migration ve manuel inceleme okunabilir kalir.
/// </summary>
internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    /// <summary>
    /// Ticket tablosu kolonlarini, indexlerini ve xmin concurrency token'ini tanimlar.
    /// Redis lock uygulama seviyesinde, xmin ise PostgreSQL yazma cakismalarinda ikinci koruma katmanidir.
    /// </summary>
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Id)
            .ValueGeneratedNever();

        builder.Property(ticket => ticket.EventId)
            .IsRequired();

        builder.Property(ticket => ticket.Seat)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ticket => ticket.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ticket => ticket.CreatedAt)
            .IsRequired();

        builder.Property(ticket => ticket.UpdatedAt)
            .IsRequired();

        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(ticket => new { ticket.EventId, ticket.Status });

        builder.HasIndex(ticket => ticket.LockedByUserId)
            .HasFilter("locked_by_user_id IS NOT NULL");
    }
}
