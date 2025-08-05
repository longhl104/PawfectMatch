using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Longhl104.PawfectMatch.Utils;

/// <summary>
/// Generic design-time DbContext factory for PostgreSQL with PostGIS support.
/// This factory enables EF Core migrations to work without requiring runtime dependencies like AWS credentials.
/// </summary>
/// <typeparam name="TContext">The DbContext type</typeparam>
public class PostgreSqlDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly string _defaultConnectionString;
    private readonly string _environmentVariableName;

    /// <summary>
    /// Initializes a new instance of the PostgreSqlDesignTimeDbContextFactory
    /// </summary>
    /// <param name="defaultConnectionString">Fallback connection string for local development</param>
    /// <param name="environmentVariableName">Environment variable name to check for connection string (default: "ConnectionStrings__DefaultConnection")</param>
    public PostgreSqlDesignTimeDbContextFactory(
        string defaultConnectionString = "Host=localhost;Port=5432;Database=pawfectmatch_dev;Username=postgres;Password=password;",
        string environmentVariableName = "ConnectionStrings__DefaultConnection")
    {
        _defaultConnectionString = defaultConnectionString;
        _environmentVariableName = environmentVariableName;
    }

    /// <summary>
    /// Creates a DbContext instance for design-time operations
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured DbContext instance</returns>
    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        // Try to get connection string from environment variable first
        var connectionString = Environment.GetEnvironmentVariable(_environmentVariableName);

        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback to default connection string for design-time operations
            connectionString = _defaultConnectionString;
        }

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.UseNetTopologySuite();
        });

        // Use reflection to create instance since we can't use generic constraints with constructors
        return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
    }
}
