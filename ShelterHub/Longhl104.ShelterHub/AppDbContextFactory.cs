using Microsoft.EntityFrameworkCore.Design;
using Longhl104.PawfectMatch.Utils;

namespace Longhl104.ShelterHub;

/// <summary>
/// Design-time DbContext factory for ShelterHub AppDbContext.
/// This enables EF Core migrations to work without requiring AWS credentials.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private readonly PostgreSqlDesignTimeDbContextFactory<AppDbContext> _factory;

    public AppDbContextFactory()
    {
        _factory = new PostgreSqlDesignTimeDbContextFactory<AppDbContext>(
            defaultConnectionString: "Host=localhost;Port=5432;Database=pawfectmatch_dev;Username=postgres;Password=password;",
            environmentVariableName: "ConnectionStrings__DefaultConnection");
    }

    public AppDbContext CreateDbContext(string[] args)
    {
        return _factory.CreateDbContext(args);
    }
}
