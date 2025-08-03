using Longhl104.ShelterHub.Models.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Longhl104.ShelterHub.Data.SeedData;

public static class BreedsSeedData
{
    public static void SeedBreeds(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PetBreed>().HasData(
            // Dog breeds
            new PetBreed { BreedId = 1, Name = "Mixed Breed", SpeciesId = 1 },
            new PetBreed { BreedId = 2, Name = "Labrador Retriever", SpeciesId = 1 },
            new PetBreed { BreedId = 3, Name = "Golden Retriever", SpeciesId = 1 },
            new PetBreed { BreedId = 4, Name = "German Shepherd", SpeciesId = 1 },
            new PetBreed { BreedId = 5, Name = "French Bulldog", SpeciesId = 1 },
            new PetBreed { BreedId = 6, Name = "Bulldog", SpeciesId = 1 },
            new PetBreed { BreedId = 7, Name = "Poodle", SpeciesId = 1 },
            new PetBreed { BreedId = 8, Name = "Beagle", SpeciesId = 1 },
            new PetBreed { BreedId = 9, Name = "Rottweiler", SpeciesId = 1 },
            new PetBreed { BreedId = 10, Name = "Yorkshire Terrier", SpeciesId = 1 },
            new PetBreed { BreedId = 11, Name = "Chihuahua", SpeciesId = 1 },
            new PetBreed { BreedId = 12, Name = "Border Collie", SpeciesId = 1 },
            new PetBreed { BreedId = 13, Name = "Australian Shepherd", SpeciesId = 1 },
            new PetBreed { BreedId = 14, Name = "Siberian Husky", SpeciesId = 1 },
            new PetBreed { BreedId = 15, Name = "Boxer", SpeciesId = 1 },

            // Cat breeds
            new PetBreed { BreedId = 16, Name = "Domestic Shorthair", SpeciesId = 2 },
            new PetBreed { BreedId = 17, Name = "Domestic Longhair", SpeciesId = 2 },
            new PetBreed { BreedId = 18, Name = "Persian", SpeciesId = 2 },
            new PetBreed { BreedId = 19, Name = "Maine Coon", SpeciesId = 2 },
            new PetBreed { BreedId = 20, Name = "Ragdoll", SpeciesId = 2 },
            new PetBreed { BreedId = 21, Name = "British Shorthair", SpeciesId = 2 },
            new PetBreed { BreedId = 22, Name = "Siamese", SpeciesId = 2 },
            new PetBreed { BreedId = 23, Name = "American Shorthair", SpeciesId = 2 },
            new PetBreed { BreedId = 24, Name = "Russian Blue", SpeciesId = 2 },
            new PetBreed { BreedId = 25, Name = "Bengal", SpeciesId = 2 },

            // Rabbit breeds
            new PetBreed { BreedId = 26, Name = "Mixed Breed", SpeciesId = 3 },
            new PetBreed { BreedId = 27, Name = "Holland Lop", SpeciesId = 3 },
            new PetBreed { BreedId = 28, Name = "Netherland Dwarf", SpeciesId = 3 },
            new PetBreed { BreedId = 29, Name = "Mini Rex", SpeciesId = 3 },
            new PetBreed { BreedId = 30, Name = "Lionhead", SpeciesId = 3 },

            // Bird breeds
            new PetBreed { BreedId = 31, Name = "Budgerigar", SpeciesId = 4 },
            new PetBreed { BreedId = 32, Name = "Cockatiel", SpeciesId = 4 },
            new PetBreed { BreedId = 33, Name = "Canary", SpeciesId = 4 },
            new PetBreed { BreedId = 34, Name = "Lovebird", SpeciesId = 4 },
            new PetBreed { BreedId = 35, Name = "Conure", SpeciesId = 4 },

            // Guinea Pig breeds
            new PetBreed { BreedId = 36, Name = "American Guinea Pig", SpeciesId = 5 },
            new PetBreed { BreedId = 37, Name = "Peruvian Guinea Pig", SpeciesId = 5 },
            new PetBreed { BreedId = 38, Name = "Abyssinian Guinea Pig", SpeciesId = 5 },

            // Hamster breeds
            new PetBreed { BreedId = 39, Name = "Syrian Hamster", SpeciesId = 6 },
            new PetBreed { BreedId = 40, Name = "Dwarf Hamster", SpeciesId = 6 },

            // Ferret breeds
            new PetBreed { BreedId = 41, Name = "Domestic Ferret", SpeciesId = 7 }
        );
    }
}
