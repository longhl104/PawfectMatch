using Longhl104.ShelterHub.Models.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Longhl104.ShelterHub.Data.SeedData;

public static class SpeciesSeedData
{
    public static void SeedSpecies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PetSpecies>().HasData(
            new PetSpecies { SpeciesId = 1, Name = "Dog" },
            new PetSpecies { SpeciesId = 2, Name = "Cat" },
            new PetSpecies { SpeciesId = 3, Name = "Rabbit" },
            new PetSpecies { SpeciesId = 4, Name = "Bird" },
            new PetSpecies { SpeciesId = 5, Name = "Guinea Pig" },
            new PetSpecies { SpeciesId = 6, Name = "Hamster" },
            new PetSpecies { SpeciesId = 7, Name = "Ferret" },
            new PetSpecies { SpeciesId = 8, Name = "Other" }
        );
    }
}
