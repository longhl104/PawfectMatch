using Longhl104.ShelterHub.Models.PostgreSql;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Longhl104.ShelterHub.Data.SeedData;

public static class BreedsSeedData
{
    public static void SeedBreeds(ModelBuilder modelBuilder)
    {
        var breeds = new List<PetBreed>();
        var breedId = 1;

        // Load dog breeds from JSON file
        var dogBreedsJson = File.ReadAllText("Data/SeedData/dog-breeds.json");
        var dogBreedNames = JsonSerializer.Deserialize<string[]>(dogBreedsJson) ?? [];

        // Add Mixed Breed first
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = 1 });

        // Add all dog breeds from JSON
        foreach (var dogBreedName in dogBreedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = dogBreedName, SpeciesId = 1 });
        }

        // Add "Other" for dogs
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 1 });

        // Cat breeds
        var catBreeds = new[]
        {
            "Domestic Shorthair", "Domestic Longhair", "Persian", "Maine Coon", "Ragdoll",
            "British Shorthair", "Siamese", "American Shorthair", "Russian Blue", "Bengal"
        };

        foreach (var catBreed in catBreeds)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = catBreed, SpeciesId = 2 });
        }
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 2 });

        // Rabbit breeds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = 3 });
        var rabbitBreeds = new[] { "Holland Lop", "Netherland Dwarf", "Mini Rex", "Lionhead" };
        foreach (var rabbitBreed in rabbitBreeds)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = rabbitBreed, SpeciesId = 3 });
        }
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 3 });

        // Bird breeds
        var birdBreeds = new[] { "Budgerigar", "Cockatiel", "Canary", "Lovebird", "Conure" };
        foreach (var birdBreed in birdBreeds)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = birdBreed, SpeciesId = 4 });
        }
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 4 });

        // Guinea Pig breeds
        var guineaPigBreeds = new[] { "American Guinea Pig", "Peruvian Guinea Pig", "Abyssinian Guinea Pig" };
        foreach (var guineaPigBreed in guineaPigBreeds)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = guineaPigBreed, SpeciesId = 5 });
        }
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 5 });

        // Hamster breeds
        var hamsterBreeds = new[] { "Syrian Hamster", "Dwarf Hamster" };
        foreach (var hamsterBreed in hamsterBreeds)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = hamsterBreed, SpeciesId = 6 });
        }
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 6 });

        // Ferret breeds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Domestic Ferret", SpeciesId = 7 });
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 7 });

        // Other species breeds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = 8 });
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 8 });

        modelBuilder.Entity<PetBreed>().HasData(breeds.ToArray());
    }
}
