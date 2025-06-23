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
    /// Lambda function handler for CloudFormation custom resource to install PostGIS extensions
    /// </summary>
    /// <param name="input">CloudFormation custom resource event</param>
    /// <param name="context">Lambda context</param>
    /// <returns>CloudFormation custom resource response</returns>
    public async Task<CustomResourceResponse> FunctionHandler(CustomResourceRequest input, ILambdaContext context)
    {
        context.Logger.LogInformation($"Custom resource request type: {input.RequestType}");
        context.Logger.LogInformation($"Resource properties: {JsonSerializer.Serialize(input.ResourceProperties)}");

        var response = new CustomResourceResponse
        {
            Status = "SUCCESS",
            Reason = "",
            PhysicalResourceId = input.PhysicalResourceId ?? "PostGISInstaller",
            StackId = input.StackId,
            RequestId = input.RequestId,
            LogicalResourceId = input.LogicalResourceId,
            Data = new Dictionary<string, object>()
        };

        try
        {
            // Only install PostGIS on Create and Update events
            if (input.RequestType == "Create" || input.RequestType == "Update")
            {
                var result = await InstallPostGIS(input.ResourceProperties, context);
                response.Data["PostGISVersion"] = result.PostGISVersion;
                response.Data["Message"] = result.Message;
                response.Reason = result.Message;
            }
            else if (input.RequestType == "Delete")
            {
                // For delete events, we don't need to do anything with PostGIS
                context.Logger.LogInformation("Delete event - no action needed for PostGIS");
                response.Reason = "Delete completed - no action needed";
            }

            return response;
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing custom resource: {ex.Message}");
            response.Status = "FAILED";
            response.Reason = $"Error: {ex.Message}";
            return response;
        }
    }

    private async Task<PostGISInstallResult> InstallPostGIS(Dictionary<string, object> properties, ILambdaContext context)
    {
        var databaseHost = properties["DatabaseHost"].ToString()!;
        var databaseName = properties["DatabaseName"].ToString()!;
        var secretArn = properties["SecretArn"].ToString()!;

        context.Logger.LogInformation($"Starting PostGIS installation for database: {databaseHost}");

        // Get database credentials from Secrets Manager
        var secretRequest = new GetSecretValueRequest
        {
            SecretId = secretArn
        };

        var secretResponse = await _secretsManager.GetSecretValueAsync(secretRequest);
        var secret = JsonSerializer.Deserialize<DatabaseSecret>(secretResponse.SecretString);

        if (secret == null)
        {
            throw new Exception("Failed to deserialize database secret");
        }

        context.Logger.LogInformation("Successfully retrieved database credentials from Secrets Manager");

        // Build connection string
        var connectionString = $"Host={databaseHost};Database={databaseName};Username={secret.Username};Password={secret.Password};Port=5432;SSL Mode=Require;Trust Server Certificate=true;";

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

        return new PostGISInstallResult
        {
            PostGISVersion = version?.ToString() ?? "Unknown",
            Message = $"PostGIS installed successfully. Version: {version}"
        };
    }
}

// CloudFormation Custom Resource request structure
public class CustomResourceRequest
{
    public string RequestType { get; set; } = string.Empty; // Create, Update, Delete
    public string ResponseURL { get; set; } = string.Empty;
    public string StackId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string LogicalResourceId { get; set; } = string.Empty;
    public string? PhysicalResourceId { get; set; }
    public Dictionary<string, object> ResourceProperties { get; set; } = new();
    public Dictionary<string, object>? OldResourceProperties { get; set; }
}

// CloudFormation Custom Resource response structure
public class CustomResourceResponse
{
    public string Status { get; set; } = string.Empty; // SUCCESS or FAILED
    public string Reason { get; set; } = string.Empty;
    public string PhysicalResourceId { get; set; } = string.Empty;
    public string StackId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string LogicalResourceId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class PostGISInstallResult
{
    public string PostGISVersion { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
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
