namespace Longhl104.ShelterHub.Models;

/// <summary>
/// Shelter information stored in DynamoDB
/// </summary>
public class Shelter
{
    /// <summary>
    /// Unique shelter ID (Primary Key)
    /// </summary>
    public required Guid ShelterId { get; set; }

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

/// <summary>
/// Request model for querying shelter with specific attributes
/// </summary>
public class QueryShelterRequest
{
    /// <summary>
    /// List of attributes to retrieve from the shelter
    /// If null or empty, all attributes will be returned
    /// </summary>
    public List<string>? AttributesToGet { get; set; }
}
