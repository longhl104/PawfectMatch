using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace InstallPostgis;

public class Function
{
    private readonly AmazonSecretsManagerClient _secretsManagerClient;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        _secretsManagerClient = new AmazonSecretsManagerClient();
    }

    /// <summary>
    /// Constructor for testing purposes
    /// </summary>
    public Function(AmazonSecretsManagerClient secretsManagerClient)
    {
        _secretsManagerClient = secretsManagerClient;
    }

    /// <summary>
    /// A Lambda function to install PostGIS extension in PostgreSQL database
    /// </summary>
    /// <param name="input">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<PostgisInstallationResponse> FunctionHandler(PostgisInstallationRequest input, ILambdaContext context)
    {
        var logger = context.Logger;
        logger.LogInformation($"Installing PostGIS extension for stage: {input.Stage}");

        try
        {
            // Get database credentials from Secrets Manager
            var secretArn = $"pawfectmatch-{input.Stage}-db-credentials";
            logger.LogInformation($"Retrieving database credentials from secret: {secretArn}");

            var secretResponse = await _secretsManagerClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretArn
            });


            var secret = JsonSerializer.Deserialize<DatabaseSecret>(secretResponse.SecretString) ?? throw new Exception("Failed to deserialize database secret");

            // Build connection string
            var connectionString = $"Host={secret.Host};Port={secret.Port};Database={secret.DbName};Username={secret.Username};Password={secret.Password};SSL Mode=Require;Trust Server Certificate=true";

            logger.LogInformation($"Connecting to database: {secret.Host}:{secret.Port}/{secret.DbName}");

            // Connect to database and install PostGIS
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            logger.LogInformation("Database connection established successfully");

            // Check if PostGIS extension already exists
            var checkExtensionCommand = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM pg_extension WHERE extname = 'postgis');", connection);
            var extensionExists = (bool)(await checkExtensionCommand.ExecuteScalarAsync() ?? false);

            if (extensionExists)
            {
                logger.LogInformation("PostGIS extension already exists");
                return new PostgisInstallationResponse
                {
                    Success = true,
                    Message = "PostGIS extension already installed",
                    ExtensionVersion = await GetPostgisVersion(connection)
                };
            }

            // Install PostGIS extension
            logger.LogInformation("Installing PostGIS extension...");
            var installCommand = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS postgis;", connection);
            await installCommand.ExecuteNonQueryAsync();

            // Verify installation
            var verifyCommand = new NpgsqlCommand("SELECT PostGIS_Version();", connection);
            var version = await verifyCommand.ExecuteScalarAsync() as string;

            logger.LogInformation($"PostGIS extension installed successfully. Version: {version}");

            return new PostgisInstallationResponse
            {
                Success = true,
                Message = "PostGIS extension installed successfully",
                ExtensionVersion = version
            };
        }
        catch (Exception ex)
        {
            logger.LogError($"Error installing PostGIS extension: {ex.Message}");
            logger.LogError($"Stack trace: {ex.StackTrace}");

            return new PostgisInstallationResponse
            {
                Success = false,
                Message = $"Failed to install PostGIS extension: {ex.Message}",
                ExtensionVersion = null
            };
        }
    }

    private async Task<string?> GetPostgisVersion(NpgsqlConnection connection)
    {
        try
        {
            var versionCommand = new NpgsqlCommand("SELECT PostGIS_Version();", connection);
            return await versionCommand.ExecuteScalarAsync() as string;
        }
        catch
        {
            return null;
        }
    }
}

public class PostgisInstallationRequest
{
    public string Stage { get; set; } = string.Empty;
}

public class PostgisInstallationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExtensionVersion { get; set; }
}

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
