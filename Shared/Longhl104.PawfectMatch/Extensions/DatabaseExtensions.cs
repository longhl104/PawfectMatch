using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Longhl104.PawfectMatch.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Configures PostgreSQL database context using AWS Secrets Manager for connection string
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="secretArnConfigKey">Configuration key containing the secret ARN (default: "Database:SecretArn")</param>
    /// <returns>The service collection for chaining</returns>
    public static async Task<IServiceCollection> AddPawfectMatchPostgreSqlAsync<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string secretArnConfigKey = "Database:SecretArn")
        where TContext : DbContext
    {
        var secretArn = configuration[secretArnConfigKey];
        if (string.IsNullOrEmpty(secretArn))
        {
            throw new InvalidOperationException($"Configuration key '{secretArnConfigKey}' is required");
        }

        // Get connection string from Secrets Manager
        var secretsManager = new AmazonSecretsManagerClient();
        var connectionString = await GetConnectionStringFromSecret(secretsManager, secretArn);

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.UseNetTopologySuite()));

        // Register the secrets manager client for DI
        services.AddSingleton<IAmazonSecretsManager, AmazonSecretsManagerClient>();

        return services;
    }

    /// <summary>
    /// Helper method to get connection string from AWS Secrets Manager
    /// </summary>
    /// <param name="secretsManager">The Secrets Manager client</param>
    /// <param name="secretArn">The ARN of the secret</param>
    /// <returns>PostgreSQL connection string</returns>
    private static async Task<string> GetConnectionStringFromSecret(IAmazonSecretsManager secretsManager, string secretArn)
    {
        var request = new GetSecretValueRequest
        {
            SecretId = secretArn
        };

        var response = await secretsManager.GetSecretValueAsync(request);
        var secret = JsonSerializer.Deserialize<DatabaseSecret>(response.SecretString)
            ?? throw new InvalidOperationException("Failed to deserialize database secret");

        return $"Host={secret.Host};Port={secret.Port};Database={secret.DbName};Username={secret.Username};Password={secret.Password}";
    }
}

/// <summary>
/// Class to deserialize the database secret from AWS Secrets Manager
/// </summary>
public class DatabaseSecret
{
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("dbname")]
    public string DbName { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }
}
