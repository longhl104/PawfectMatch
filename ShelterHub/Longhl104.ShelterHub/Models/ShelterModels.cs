namespace Longhl104.ShelterHub.Models;

/// <summary>
/// Request model for creating a shelter admin profile
/// </summary>
public class CreateShelterAdminRequest
{
    /// <summary>
    /// The user ID from Cognito
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the shelter
    /// </summary>
    public string ShelterName { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number for the shelter
    /// </summary>
    public string ShelterContactNumber { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the shelter
    /// </summary>
    public string ShelterAddress { get; set; } = string.Empty;

    /// <summary>
    /// Optional website URL for the shelter
    /// </summary>
    public string? ShelterWebsiteUrl { get; set; }

    /// <summary>
    /// Optional Australian Business Number
    /// </summary>
    public string? ShelterAbn { get; set; }

    /// <summary>
    /// Optional description of the shelter
    /// </summary>
    public string? ShelterDescription { get; set; }
}

/// <summary>
/// Response model for shelter admin operations
/// </summary>
public class ShelterAdminResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The shelter admin user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The created shelter ID
    /// </summary>
    public string? ShelterId { get; set; }
}

/// <summary>
/// Shelter admin profile stored in DynamoDB
/// </summary>
public class ShelterAdmin
{
    /// <summary>
    /// The user ID from Cognito (Primary Key)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The associated shelter ID
    /// </summary>
    public string ShelterId { get; set; } = string.Empty;

    /// <summary>
    /// When the profile was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the profile was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shelter information stored in DynamoDB
/// </summary>
public class Shelter
{
    /// <summary>
    /// Unique shelter ID (Primary Key)
    /// </summary>
    public string ShelterId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the shelter
    /// </summary>
    public string ShelterName { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number for the shelter
    /// </summary>
    public string ShelterContactNumber { get; set; } = string.Empty;

    /// <summary>
    /// Physical address of the shelter
    /// </summary>
    public string ShelterAddress { get; set; } = string.Empty;

    /// <summary>
    /// Optional website URL for the shelter
    /// </summary>
    public string? ShelterWebsiteUrl { get; set; }

    /// <summary>
    /// Optional Australian Business Number
    /// </summary>
    public string? ShelterAbn { get; set; }

    /// <summary>
    /// Optional description of the shelter
    /// </summary>
    public string? ShelterDescription { get; set; }

    /// <summary>
    /// Whether the shelter is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the shelter was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the shelter was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
