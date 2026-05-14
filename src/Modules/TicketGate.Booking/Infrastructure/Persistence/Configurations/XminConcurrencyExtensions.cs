using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TicketGate.Booking.Infrastructure.Persistence.Configurations;

/// <summary>
/// Npgsql EF Core 10 icin xmin concurrency konfigurasyonunu tek noktada toplar.
/// Eski UseXminAsConcurrencyToken API'si kaldirildigi icin ayni davranis shadow row-version property ile saglanir.
/// </summary>
internal static class XminConcurrencyExtensions
{
    /// <summary>
    /// Entity'ye PostgreSQL xmin sistem kolonunu optimistic concurrency token olarak ekler.
    /// uint row-version konfigurasyonu Npgsql convention'i tarafindan fiziksel xmin kolonuna baglanir.
    /// </summary>
    public static EntityTypeBuilder<TEntity> UseXminAsConcurrencyToken<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<uint>("xmin")
            .IsRowVersion();

        return builder;
    }
}
