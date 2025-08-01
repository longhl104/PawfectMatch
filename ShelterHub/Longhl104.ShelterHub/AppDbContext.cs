using Longhl104.ShelterHub.Models.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Longhl104.ShelterHub;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Shelter> Shelters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shelter_hub");

        modelBuilder.Entity<Shelter>()
            .ToTable("shelters")
            .HasKey(s => s.ShelterId);
    }
}
