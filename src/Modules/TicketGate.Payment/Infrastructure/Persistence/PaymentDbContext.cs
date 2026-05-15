using Microsoft.EntityFrameworkCore;
using TicketGate.Payment.Infrastructure.Outbox;
using PaymentEntity = TicketGate.Payment.Domain.Entities.Payment;

namespace TicketGate.Payment.Infrastructure.Persistence;

/// <summary>
/// Payment modulu EF Core context'i. Schema izolasyonu ile diger modullerin tablolarina direkt erisim engellenir.
/// Outbox tablosu ayni context'te tutulur; Payment ve OutboxMessage icin tek transaction garantisi saglanir.
/// </summary>
public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Payment schema'sini ve entity konfigurasyonlarini uygular.
    /// Moduler monolith icinde outbox atomikligi ayni DbContext sinirinda korunur.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(PaymentSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }
}
