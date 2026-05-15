using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = TicketGate.Payment.Domain.Entities.Payment;

namespace TicketGate.Payment.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core payment tablo konfigurasyonu.
/// IdempotencyKey unique index ile duplicate odeme engellenir; Status string olarak saklanir.
/// </summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    /// <summary>
    /// Payment kolonlarini, indexlerini ve enum conversion davranisini tanimlar.
    /// Idempotency unique index uygulama seviyesindeki kontrolu veritabani seviyesinde de garanti eder.
    /// </summary>
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Amount)
            .HasColumnType("numeric(10,2)");

        builder.Property(payment => payment.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(payment => payment.ExternalPaymentId)
            .HasMaxLength(200);

        builder.Property(payment => payment.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(payment => payment.IdempotencyKey).IsUnique();
        builder.HasIndex(payment => payment.TicketId);
        builder.HasIndex(payment => payment.UserId);
    }
}
