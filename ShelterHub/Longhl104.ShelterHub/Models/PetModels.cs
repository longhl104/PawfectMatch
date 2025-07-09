using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Longhl104.ShelterHub.Models;

/// <summary>
/// Pet status enumeration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetStatus
{
    [AmbientValue("Available")]
    Available,
    [AmbientValue("Pending")]
    Pending,
    [AmbientValue("Adopted")]
    Adopted,
    [AmbientValue("MedicalHold")]
    MedicalHold
}

/// <summary>
/// Pet model representing a pet in the shelter
/// </summary>
public class Pet
{
    /// <summary>
    /// Unique identifier for the pet
    /// </summary>
    public Guid PetId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the pet
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Species of the pet (Dog, Cat, etc.)
    /// </summary>
    public string Species { get; set; } = string.Empty;

    /// <summary>
    /// Breed of the pet
    /// </summary>
    public string Breed { get; set; } = string.Empty;

    /// <summary>
    /// Age of the pet in years
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gender of the pet (Male, Female)
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the pet
    /// </summary>
    public PetStatus Status { get; set; } = PetStatus.Available;

    /// <summary>
    /// Description of the pet
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Date when the pet was added to the shelter
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the shelter that owns this pet
    /// </summary>
    public Guid ShelterId { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Request model for creating a new pet
/// </summary>
public class CreatePetRequest
{
    /// <summary>
    /// Name of the pet
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Species of the pet (Dog, Cat, etc.)
    /// </summary>
    public required string Species { get; set; }

    /// <summary>
    /// Breed of the pet
    /// </summary>
    public required string Breed { get; set; }

    /// <summary>
    /// Age of the pet in years
    /// </summary>
    public required int Age { get; set; }

    /// <summary>
    /// Gender of the pet (Male, Female)
    /// </summary>
    public required string Gender { get; set; }

    /// <summary>
    /// Description of the pet
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Optional image URL for the pet
    /// </summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Response model for pet operations
/// </summary>
public class PetResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The pet data (if successful)
    /// </summary>
    public Pet? Pet { get; set; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request model for paginated pet queries
/// </summary>
public class GetPetsRequest
{
    /// <summary>
    /// Number of items to return per page (default: 10, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Token for pagination (from previous response)
    /// </summary>
    public string? NextToken { get; set; }
}

/// <summary>
/// Response model for getting multiple pets with pagination
/// </summary>
public class GetPaginatedPetsResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of pets (if successful)
    /// </summary>
    public List<Pet>? Pets { get; set; }

    /// <summary>
    /// Token for next page (null if no more pages)
    /// </summary>
    public string? NextToken { get; set; }

    /// <summary>
    /// Total count of pets in the shelter
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for getting multiple pets
/// </summary>
public class GetPetsResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of pets (if successful)
    /// </summary>
    public List<Pet>? Pets { get; set; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
