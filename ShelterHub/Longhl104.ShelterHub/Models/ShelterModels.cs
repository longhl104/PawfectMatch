namespace Longhl104.ShelterHub.Models;

/// <summary>
/// Request model for creating a shelter admin profile
/// </summary>
public class CreateShelterAdminRequest
{
    /// <summary>
    /// The user ID from Cognito
    /// </summary>
    public required Guid UserId { get; set; }

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
    public Guid UserId { get; set; }

    /// <summary>
    /// The created shelter ID
    /// </summary>
    public Guid ShelterId { get; set; }
}

/// <summary>
/// Shelter admin profile stored in DynamoDB
/// </summary>
public class ShelterAdmin
{
    /// <summary>
    /// The user ID from Cognito (Primary Key)
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// The associated shelter ID
    /// </summary>
    public required Guid ShelterId { get; set; }

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

/// <summary>
/// Pet status enumeration
/// </summary>
public enum PetStatus
{
    Available,
    Pending,
    Adopted,
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
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

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
