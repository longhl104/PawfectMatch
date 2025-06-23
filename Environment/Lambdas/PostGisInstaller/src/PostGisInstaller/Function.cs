using Amazon.Lambda.Core;
using System.Text.Json;
using Npgsql;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PostGisInstaller;

public class Function
{
  private readonly IAmazonSecretsManager _secretsManager;

  public Function()
  {
    _secretsManager = new AmazonSecretsManagerClient();
  }

  // Constructor for dependency injection (useful for testing)
  public Function(IAmazonSecretsManager secretsManager)
  {
    _secretsManager = secretsManager;
  }

  /// <summary>
  /// Lambda function handler to install PostGIS extensions on PostgreSQL database
  /// </summary>
  /// <param name="input">The event for the Lambda function handler to process.</param>
  /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
  /// <returns>Response indicating success or failure</returns>
  public async Task<PostGisInstallerResponse> FunctionHandler(PostGisInstallerRequest input, ILambdaContext context)
  {
    context.Logger.LogInformation($"Starting PostGIS installation for database: {input.DatabaseHost}");

    try
    {
      // Get database credentials from Secrets Manager
      var secretRequest = new GetSecretValueRequest
      {
        SecretId = input.SecretArn
      };

      var secretResponse = await _secretsManager.GetSecretValueAsync(secretRequest);
      var secret = JsonSerializer.Deserialize<DatabaseSecret>(secretResponse.SecretString);

      if (secret == null)
      {
        throw new Exception("Failed to deserialize database secret");
      }

      context.Logger.LogInformation("Successfully retrieved database credentials from Secrets Manager");

      // Build connection string
      var connectionString = $"Host={input.DatabaseHost};Database={input.DatabaseName};Username={secret.Username};Password={secret.Password};Port=5432;SSL Mode=Require;Trust Server Certificate=true;";

      // Connect to database and install PostGIS
      await using var connection = new NpgsqlConnection(connectionString);
      await connection.OpenAsync();

      context.Logger.LogInformation("Connected to PostgreSQL database");

      // Install PostGIS extensions
      await using var command = connection.CreateCommand();

      // Create PostGIS extension
      command.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis;";
      await command.ExecuteNonQueryAsync();
      context.Logger.LogInformation("PostGIS extension installed/verified");

      // Create PostGIS topology extension
      command.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis_topology;";
      await command.ExecuteNonQueryAsync();
      context.Logger.LogInformation("PostGIS topology extension installed/verified");

      // Verify installation by checking PostGIS version
      command.CommandText = "SELECT PostGIS_Version();";
      var version = await command.ExecuteScalarAsync();
      context.Logger.LogInformation($"PostGIS version: {version}");

      await connection.CloseAsync();

      return new PostGisInstallerResponse
      {
        StatusCode = 200,
        Message = $"PostGIS installed successfully. Version: {version}",
        Success = true
      };
    }
    catch (Exception ex)
    {
      context.Logger.LogError($"Error installing PostGIS: {ex.Message}");
      return new PostGisInstallerResponse
      {
        StatusCode = 500,
        Message = $"Error installing PostGIS: {ex.Message}",
        Success = false
      };
    }
  }
}

public class PostGisInstallerRequest
{
  public string DatabaseHost { get; set; } = string.Empty;
  public string DatabaseName { get; set; } = string.Empty;
  public string SecretArn { get; set; } = string.Empty;
}

public class PostGisInstallerResponse
{
  public int StatusCode { get; set; }
  public string Message { get; set; } = string.Empty;
  public bool Success { get; set; }
}

public class DatabaseSecret
{
  public string Username { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string Engine { get; set; } = string.Empty;
  public string Host { get; set; } = string.Empty;
  public int Port { get; set; }
  public string DbInstanceIdentifier { get; set; } = string.Empty;
}
