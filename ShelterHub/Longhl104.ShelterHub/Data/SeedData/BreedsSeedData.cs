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

        // Load cat breeds from JSON file
        var catBreedsJson = File.ReadAllText("Data/SeedData/cat-breeds.json");
        var catBreedNames = JsonSerializer.Deserialize<string[]>(catBreedsJson) ?? [];

        // Add common cat breeds first
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Domestic Shorthair", SpeciesId = 2 });
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Domestic Longhair", SpeciesId = 2 });

        // Add all cat breeds from JSON
        foreach (var catBreedName in catBreedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = catBreedName, SpeciesId = 2 });
        }

        // Add "Other" for cats
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 2 });

        // Load rabbit breeds from JSON file
        var rabbitBreedsJson = File.ReadAllText("Data/SeedData/rabbit-breeds.json");
        var rabbitBreedNames = JsonSerializer.Deserialize<string[]>(rabbitBreedsJson) ?? [];

        // Add Mixed Breed first for rabbits
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = 3 });

        // Add all rabbit breeds from JSON
        foreach (var rabbitBreedName in rabbitBreedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = rabbitBreedName, SpeciesId = 3 });
        }

        // Add "Other" for rabbits
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 3 });

        // Load bird breeds from JSON file
        var birdBreedsJson = File.ReadAllText("Data/SeedData/bird-breeds.json");
        var birdBreedNames = JsonSerializer.Deserialize<string[]>(birdBreedsJson) ?? [];

        // Add all bird breeds from JSON
        foreach (var birdBreedName in birdBreedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = birdBreedName, SpeciesId = 4 });
        }

        // Add "Other" for birds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 4 });

        // Load guinea pig breeds from JSON file
        var guineaPigBreedsJson = File.ReadAllText("Data/SeedData/guinea-pig-breeds.json");
        var guineaPigBreedNames = JsonSerializer.Deserialize<string[]>(guineaPigBreedsJson) ?? [];

        // Add all guinea pig breeds from JSON
        foreach (var guineaPigBreedName in guineaPigBreedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = guineaPigBreedName, SpeciesId = 5 });
        }

        // Add "Other" for guinea pigs
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 5 });

        // Load hamster breeds from JSON file
        var hamsterBreedsJson = File.ReadAllText("Data/SeedData/hamster-breeds.json");
        var hamsterBreedNames = JsonSerializer.Deserialize<string[]>(hamsterBreedsJson) ?? [];

        // Add all hamster breeds from JSON
        foreach (var hamsterBreedName in hamsterBreedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = hamsterBreedName, SpeciesId = 6 });
        }

        // Add "Other" for hamsters
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 6 });

        // Ferret breeds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Domestic Ferret", SpeciesId = 7 });
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 7 });

        // Other species breeds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = 8 });
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 8 });

        modelBuilder.Entity<PetBreed>().HasData([.. breeds]);
    }
}
