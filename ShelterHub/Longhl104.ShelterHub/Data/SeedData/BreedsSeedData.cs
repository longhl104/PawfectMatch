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

        // Define species configurations
        var speciesConfigs = new[]
        {
            new { SpeciesId = 1, JsonFile = "dog-breeds.json", CommonBreeds = Array.Empty<string>() },
            new { SpeciesId = 2, JsonFile = "cat-breeds.json", CommonBreeds = new[] { "Domestic Shorthair", "Domestic Longhair" } },
            new { SpeciesId = 3, JsonFile = "rabbit-breeds.json", CommonBreeds = Array.Empty<string>() },
            new { SpeciesId = 4, JsonFile = "bird-breeds.json", CommonBreeds = Array.Empty<string>() },
            new { SpeciesId = 5, JsonFile = "guinea-pig-breeds.json", CommonBreeds = Array.Empty<string>() },
            new { SpeciesId = 6, JsonFile = "hamster-breeds.json", CommonBreeds = Array.Empty<string>() },
            new { SpeciesId = 7, JsonFile = "ferret-breeds.json", CommonBreeds = new[] { "Domestic Ferret" } }
        };

        // Process each species
        foreach (var config in speciesConfigs)
        {
            breedId = AddBreedsForSpecies(breeds, breedId, config.SpeciesId, config.JsonFile, config.CommonBreeds);
        }

        // Add other species breeds
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = 8 });
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = 8 });

        modelBuilder.Entity<PetBreed>().HasData([.. breeds]);
    }

    private static int AddBreedsForSpecies(List<PetBreed> breeds, int startingBreedId, int speciesId, string jsonFileName, string[] commonBreeds)
    {
        var breedId = startingBreedId;

        // Load breeds from JSON file
        var breedsJson = File.ReadAllText($"Data/SeedData/{jsonFileName}");
        var breedNames = JsonSerializer.Deserialize<string[]>(breedsJson) ?? [];

        // Add Mixed Breed first
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Mixed Breed", SpeciesId = speciesId });

        // Add common breeds (if any)
        foreach (var commonBreed in commonBreeds)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = commonBreed, SpeciesId = speciesId });
        }

        // Add all breeds from JSON
        foreach (var breedName in breedNames)
        {
            breeds.Add(new PetBreed { BreedId = breedId++, Name = breedName, SpeciesId = speciesId });
        }

        // Add "Other"
        breeds.Add(new PetBreed { BreedId = breedId++, Name = "Other", SpeciesId = speciesId });

        return breedId;
    }
}
