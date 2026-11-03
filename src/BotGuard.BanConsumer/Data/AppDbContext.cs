using BotGuard.BanConsumer.Models;
using Microsoft.EntityFrameworkCore;

namespace BotGuard.BanConsumer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>()
            .HasIndex(i => new { i.IpAddress, i.DetectedAtUtc }).IsUnique();
    }
}