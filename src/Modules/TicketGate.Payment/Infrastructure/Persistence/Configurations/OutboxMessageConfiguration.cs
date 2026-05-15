using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketGate.Payment.Infrastructure.Outbox;

namespace TicketGate.Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core outbox tablo konfigurasyonu.
/// ProcessedAt IS NULL filtreli index worker sorgu performansini optimize eder.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>
    /// Outbox kolonlarini ve polling sorgularinda kullanilan indexleri tanimlar.
    /// JSON payload jsonb olarak saklanir, islenmemis mesajlar filtreli index ile hizli bulunur.
    /// </summary>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(outbox => outbox.Id);

        builder.Property(outbox => outbox.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(outbox => outbox.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(outbox => outbox.Error)
            .HasMaxLength(1000);

        builder.HasIndex(outbox => outbox.ProcessedAt)
            .HasFilter("processed_at IS NULL");

        builder.HasIndex(outbox => outbox.RetryCount);
    }
}
