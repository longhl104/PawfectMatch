using Longhl104.ShelterHub.Models.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Longhl104.ShelterHub;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Shelter> Shelters { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<PetSpecies> PetSpecies { get; set; }
    public DbSet<PetBreed> PetBreeds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shelter_hub");

        // Configure Shelter entity
        modelBuilder.Entity<Shelter>()
            .ToTable("shelters")
            .HasKey(s => s.ShelterId);

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
            .HasIndex(p => p.IsAvailable);

        modelBuilder.Entity<PetBreed>()
            .HasIndex(b => b.SpeciesId);
    }
}
