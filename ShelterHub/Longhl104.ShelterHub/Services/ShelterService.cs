using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Longhl104.ShelterHub.Models;

namespace Longhl104.ShelterHub.Services;

/// <summary>
/// Interface for shelter data service
/// </summary>
public interface IShelterService
{
    /// <summary>
    /// Creates a shelter admin profile and associated shelter
    /// </summary>
    /// <param name="request">The shelter admin creation request</param>
    /// <returns>Response containing success status and IDs</returns>
    Task<ShelterAdminResponse> CreateShelterAdminAsync(CreateShelterAdminRequest request);

    /// <summary>
    /// Gets a shelter admin by user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The shelter admin profile or null if not found</returns>
    Task<ShelterAdmin?> GetShelterAdminAsync(Guid userId, List<string>? attributesToGet = null);

    /// <summary>
    /// Gets a shelter by shelter ID
    /// </summary>
    /// <param name="shelterId">The shelter ID</param>
    /// <returns>The shelter or null if not found</returns>
    Task<Shelter?> GetShelterAsync(Guid shelterId, List<string>? attributesToGet = null);

    /// <summary>
    /// Checks if a shelter admin exists for the given user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>True if shelter admin exists, false otherwise</returns>
    /// <remarks>
    /// This method is optimized for existence checking only. It uses ProjectionExpression
    /// to retrieve only the key attributes, minimizing data transfer and improving performance
    /// compared to retrieving the full shelter admin record.
    /// </remarks>
    Task<bool> ShelterAdminExistsAsync(Guid userId);
}

/// <summary>
/// Service for managing shelter and shelter admin data in DynamoDB
/// </summary>
public class ShelterService : IShelterService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ShelterService> _logger;
    private readonly string _shelterAdminsTableName;
    private readonly string _sheltersTableName;

    public ShelterService(
        IAmazonDynamoDB dynamoDbClient,
        IHostEnvironment environment,
        ILogger<ShelterService> logger
        )
    {
        _dynamoDbClient = dynamoDbClient;
        _environment = environment;
        _logger = logger;

        // Table names follow the CDK naming convention
        _shelterAdminsTableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-shelter-admins";
        _sheltersTableName = $"pawfectmatch-{_environment.EnvironmentName.ToLowerInvariant()}-shelter-hub-shelters";
    }

    public async Task<ShelterAdminResponse> CreateShelterAdminAsync(CreateShelterAdminRequest request)
    {
        try
        {
            _logger.LogInformation("Creating shelter admin profile for UserId: {UserId}", request.UserId);

            // Validate request
            var validationError = ValidateCreateShelterAdminRequest(request);
            if (!string.IsNullOrEmpty(validationError))
            {
                _logger.LogWarning("Validation failed for shelter admin creation: {Error}", validationError);
                return new ShelterAdminResponse
                {
                    Success = false,
                    Message = validationError,
                    UserId = request.UserId
                };
            }

            // Check if shelter admin already exists
            var shelterAdminExists = await ShelterAdminExistsAsync(request.UserId);
            if (shelterAdminExists)
            {
                _logger.LogWarning("Shelter admin already exists for UserId: {UserId}", request.UserId);
                return new ShelterAdminResponse
                {
                    Success = false,
                    Message = "Shelter admin profile already exists for this user",
                    UserId = request.UserId
                };
            }

            // Generate unique shelter ID
            var shelterId = Guid.NewGuid();

            // Create shelter record
            var shelter = new Shelter
            {
                ShelterId = shelterId,
                ShelterName = request.ShelterName,
                ShelterContactNumber = request.ShelterContactNumber,
                ShelterAddress = request.ShelterAddress,
                ShelterWebsiteUrl = request.ShelterWebsiteUrl,
                ShelterAbn = request.ShelterAbn,
                ShelterDescription = request.ShelterDescription,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create shelter admin record
            var shelterAdmin = new ShelterAdmin
            {
                UserId = request.UserId,
                ShelterId = shelterId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save both records to DynamoDB
            await SaveShelterAsync(shelter);
            await SaveShelterAdminAsync(shelterAdmin);

            _logger.LogInformation("Successfully created shelter admin profile for UserId: {UserId}, ShelterId: {ShelterId}",
                request.UserId, shelterId);

            return new ShelterAdminResponse
            {
                Success = true,
                Message = "Shelter admin profile created successfully",
                UserId = request.UserId,
                ShelterId = shelterId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shelter admin profile for UserId: {UserId}", request.UserId);
            return new ShelterAdminResponse
            {
                Success = false,
                Message = "An error occurred while creating the shelter admin profile",
                UserId = request.UserId
            };
        }
    }

    public async Task<ShelterAdmin?> GetShelterAdminAsync(Guid userId, List<string>? attributesToGet = null)
    {
        attributesToGet ??= [];
        attributesToGet.AddRange("UserId");

        try
        {
            var request = new GetItemRequest
            {
                TableName = _shelterAdminsTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["UserId"] = new AttributeValue { S = userId.ToString() }
                },
                ProjectionExpression = attributesToGet != null && attributesToGet.Count > 0
                    ? string.Join(", ", attributesToGet)
                    : null
            };

            var response = await _dynamoDbClient.GetItemAsync(request);

            if (!response.IsItemSet)
            {
                return null;
            }

            return MapToShelterAdmin(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shelter admin for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<Shelter?> GetShelterAsync(Guid shelterId, List<string>? attributesToGet = null)
    {
        attributesToGet ??= [];
        attributesToGet.AddRange("ShelterId");

        try
        {
            var request = new GetItemRequest
            {
                TableName = _sheltersTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["ShelterId"] = new AttributeValue { S = shelterId.ToString() }
                },
                ProjectionExpression = attributesToGet != null && attributesToGet.Count > 0
                    ? string.Join(", ", attributesToGet)
                    : null
            };

            var response = await _dynamoDbClient.GetItemAsync(request);

            if (!response.IsItemSet)
            {
                return null;
            }

            return MapToShelter(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shelter for ShelterId: {ShelterId}", shelterId);
            return null;
        }
    }

    public async Task<bool> ShelterAdminExistsAsync(Guid userId)
    {
        try
        {
            var request = new GetItemRequest
            {
                TableName = _shelterAdminsTableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["UserId"] = new AttributeValue { S = userId.ToString() }
                },
                // Only retrieve the key attributes to minimize data transfer
                ProjectionExpression = "UserId"
            };

            var response = await _dynamoDbClient.GetItemAsync(request);

            return response.IsItemSet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if shelter admin exists for UserId: {UserId}", userId);
            // In case of error, assume it doesn't exist to allow creation attempt
            return false;
        }
    }

    private async Task SaveShelterAsync(Shelter shelter)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["ShelterId"] = new AttributeValue { S = shelter.ShelterId.ToString() },
            ["ShelterName"] = new AttributeValue { S = shelter.ShelterName },
            ["ShelterContactNumber"] = new AttributeValue { S = shelter.ShelterContactNumber },
            ["ShelterAddress"] = new AttributeValue { S = shelter.ShelterAddress },
            ["IsActive"] = new AttributeValue { BOOL = shelter.IsActive },
            ["CreatedAt"] = new AttributeValue { S = shelter.CreatedAt.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = shelter.UpdatedAt.ToString("O") }
        };

        // Add optional fields if they have values
        if (!string.IsNullOrEmpty(shelter.ShelterWebsiteUrl))
            item["ShelterWebsiteUrl"] = new AttributeValue { S = shelter.ShelterWebsiteUrl };

        if (!string.IsNullOrEmpty(shelter.ShelterAbn))
            item["ShelterAbn"] = new AttributeValue { S = shelter.ShelterAbn };

        if (!string.IsNullOrEmpty(shelter.ShelterDescription))
            item["ShelterDescription"] = new AttributeValue { S = shelter.ShelterDescription };

        var request = new PutItemRequest
        {
            TableName = _sheltersTableName,
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(request);
    }

    private async Task SaveShelterAdminAsync(ShelterAdmin shelterAdmin)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new AttributeValue { S = shelterAdmin.UserId.ToString() },
            ["ShelterId"] = new AttributeValue { S = shelterAdmin.ShelterId.ToString() },
            ["CreatedAt"] = new AttributeValue { S = shelterAdmin.CreatedAt.ToString("O") },
            ["UpdatedAt"] = new AttributeValue { S = shelterAdmin.UpdatedAt.ToString("O") }
        };

        var request = new PutItemRequest
        {
            TableName = _shelterAdminsTableName,
            Item = item
        };

        await _dynamoDbClient.PutItemAsync(request);
    }

    private static ShelterAdmin MapToShelterAdmin(Dictionary<string, AttributeValue> item)
    {
        return new ShelterAdmin
        {
            UserId = Guid.Parse(item["UserId"].S),
            ShelterId = item.TryGetValue("ShelterId", out var shelterIdAttr) ? Guid.Parse(shelterIdAttr.S) : Guid.Empty,
            CreatedAt = item.TryGetValue("CreatedAt", out var createdAtAttr) ? DateTime.Parse(createdAtAttr.S) : DateTime.UtcNow,
            UpdatedAt = item.TryGetValue("UpdatedAt", out var updatedAtAttr) ? DateTime.Parse(updatedAtAttr.S) : DateTime.UtcNow
        };
    }

    private static Shelter MapToShelter(Dictionary<string, AttributeValue> item)
    {
        return new Shelter
        {
            ShelterId = Guid.Parse(item["ShelterId"].S),
            ShelterName = item.TryGetValue("ShelterName", out var shelterNameAttr) ? shelterNameAttr.S : string.Empty,
            ShelterContactNumber = item.TryGetValue("ShelterContactNumber", out var shelterContactNumberAttr) ? shelterContactNumberAttr.S : string.Empty,
            ShelterAddress = item.TryGetValue("ShelterAddress", out var shelterAddressAttr) ? shelterAddressAttr.S : string.Empty,
            ShelterWebsiteUrl = item.GetValueOrDefault("ShelterWebsiteUrl")?.S,
            ShelterAbn = item.GetValueOrDefault("ShelterAbn")?.S,
            ShelterDescription = item.GetValueOrDefault("ShelterDescription")?.S,
            IsActive = !item.TryGetValue("IsActive", out var isActiveAttr) || !bool.TryParse(isActiveAttr.BOOL.ToString(), out var isActive) || isActive,
            CreatedAt = item.TryGetValue("CreatedAt", out var createdAtAttr) ? DateTime.Parse(createdAtAttr.S) : DateTime.UtcNow,
            UpdatedAt = item.TryGetValue("UpdatedAt", out var updatedAtAttr) ? DateTime.Parse(updatedAtAttr.S) : DateTime.UtcNow
        };
    }

    private static string ValidateCreateShelterAdminRequest(CreateShelterAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShelterName))
            return "Shelter name is required";

        if (string.IsNullOrWhiteSpace(request.ShelterContactNumber))
            return "Shelter contact number is required";

        if (string.IsNullOrWhiteSpace(request.ShelterAddress))
            return "Shelter address is required";

        return string.Empty;
    }
}
