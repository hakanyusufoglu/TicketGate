using Microsoft.EntityFrameworkCore;
using TicketGate.Booking.Domain.Entities;

namespace TicketGate.Booking.Infrastructure.Persistence;

/// <summary>
/// Booking modulu EF Core context'i. Schema izolasyonu ile diger modullerin tablolarina direkt erisim engellenir.
/// xmin concurrency token TicketConfiguration'da aktif edilir.
/// </summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    /// <summary>
    /// Booking schema'sini ve entity konfigurasyonlarini uygular.
    /// Moduler monolith icinde her modul kendi persistence sinirini korur.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(BookingSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}
