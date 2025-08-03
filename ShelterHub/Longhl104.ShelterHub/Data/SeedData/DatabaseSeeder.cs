using Microsoft.EntityFrameworkCore;

namespace Longhl104.ShelterHub.Data.SeedData;

public static class DatabaseSeeder
{
    public static void SeedData(ModelBuilder modelBuilder)
    {
        SpeciesSeedData.SeedSpecies(modelBuilder);
        BreedsSeedData.SeedBreeds(modelBuilder);
    }
}
