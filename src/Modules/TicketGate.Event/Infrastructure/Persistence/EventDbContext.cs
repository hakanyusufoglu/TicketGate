using Microsoft.EntityFrameworkCore;
using TicketGate.Event.Domain.Entities;
using TicketGate.Event.Infrastructure;

namespace TicketGate.Event.Infrastructure.Persistence;

public sealed class EventDbContext(DbContextOptions<EventDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Event> Events => Set<Domain.Entities.Event>();

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<Performer> Performers => Set<Performer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(EventSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventDbContext).Assembly);
    }
}
