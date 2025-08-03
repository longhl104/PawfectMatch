using Longhl104.ShelterHub.Models.PostgreSql;
using Longhl104.ShelterHub.Data.SeedData;
using Microsoft.EntityFrameworkCore;

namespace Longhl104.ShelterHub;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Shelter> Shelters { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<PetSpecies> PetSpecies { get; set; }
    public DbSet<PetBreed> PetBreeds { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Enable NetTopologySuite for PostGIS support
        if (!optionsBuilder.IsConfigured)
        {
            // This will only be used during design-time operations
            optionsBuilder.UseNpgsql(connectionString =>
                connectionString.UseNetTopologySuite());
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shelter_hub");

        // Enable PostGIS extension
        modelBuilder.HasPostgresExtension("postgis");

        // Configure Shelter entity
        modelBuilder.Entity<Shelter>()
            .ToTable("shelters")
            .HasKey(s => s.ShelterId);

        // Configure PostGIS spatial data for Shelter
        modelBuilder.Entity<Shelter>()
            .Property(s => s.Location)
            .HasColumnType("geometry (point, 4326)"); // WGS84 coordinate system

        // Create spatial index for location-based queries
        modelBuilder.Entity<Shelter>()
            .HasIndex(s => s.Location)
            .HasMethod("gist"); // GiST index for spatial queries

        // Configure PetSpecies entity
        modelBuilder.Entity<PetSpecies>()
            .ToTable("pet_species")
            .HasKey(ps => ps.SpeciesId);

        // Configure PetBreed entity
        modelBuilder.Entity<PetBreed>()
            .ToTable("pet_breeds")
            .HasKey(pb => pb.BreedId);

        // Configure Pet entity
        modelBuilder.Entity<Pet>()
            .ToTable("pets")
            .HasKey(p => p.PetId);

        // Configure relationships
        modelBuilder.Entity<Pet>()
            .HasOne(p => p.Species)
            .WithMany(s => s.Pets)
            .HasForeignKey(p => p.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Pet>()
            .HasOne(p => p.Breed)
            .WithMany(b => b.Pets)
            .HasForeignKey(p => p.BreedId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Pet>()
            .HasOne(p => p.Shelter)
            .WithMany()
            .HasForeignKey(p => p.ShelterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PetBreed>()
            .HasOne(b => b.Species)
            .WithMany(s => s.Breeds)
            .HasForeignKey(b => b.SpeciesId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for better performance
        modelBuilder.Entity<Pet>()
            .HasIndex(p => p.SpeciesId);

        modelBuilder.Entity<Pet>()
            .HasIndex(p => p.BreedId);

        modelBuilder.Entity<Pet>()
            .HasIndex(p => p.ShelterId);

        modelBuilder.Entity<Pet>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<PetBreed>()
            .HasIndex(b => b.SpeciesId);

        modelBuilder.Entity<Pet>()
            .HasIndex(p => new { p.Status, p.SpeciesId, p.BreedId });

        // Seed data for species and breeds
        DatabaseSeeder.SeedData(modelBuilder);
    }
}
