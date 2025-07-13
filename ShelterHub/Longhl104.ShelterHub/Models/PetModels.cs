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
    /// Date of birth of the pet
    /// </summary>
    public DateOnly DateOfBirth { get; set; }

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

    public string? MainImageFileExtension { get; set; }
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
    /// Date of birth of the pet
    /// </summary>
    public required DateOnly DateOfBirth { get; set; }

    /// <summary>
    /// Gender of the pet (Male, Female)
    /// </summary>
    public required string Gender { get; set; }

    /// <summary>
    /// Description of the pet
    /// </summary>
    public required string Description { get; set; }
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

    /// <summary>
    /// Filter by pet status (optional - null means all statuses)
    /// </summary>
    public PetStatus? Status { get; set; }

    /// <summary>
    /// Filter by pet species (optional - null means all species)
    /// </summary>
    public string? Species { get; set; }

    /// <summary>
    /// Search by pet name (optional - case-insensitive partial match)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Search by pet breed (optional - case-insensitive partial match)
    /// </summary>
    public string? Breed { get; set; }
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

/// <summary>
/// Request model for generating presigned URLs for pet image uploads
/// </summary>
public class PresignedUrlRequest
{
    public required string BucketName { get; set; }

    public required string Key { get; set; }

    /// <summary>
    /// Content type of the file (e.g., image/jpeg)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// Response model for presigned URL generation
/// </summary>
public class PresignedUrlResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The presigned URL for uploading (if successful)
    /// </summary>
    public string? PresignedUrl { get; set; }

    /// <summary>
    /// The S3 key/path for the uploaded file
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// When the presigned URL expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request model for generating download presigned URLs
/// </summary>
public class DownloadPresignedUrlRequest
{
    /// <summary>
    /// The S3 URL to generate a download presigned URL for
    /// </summary>
    public string S3Url { get; set; } = string.Empty;
}

/// <summary>
/// Pet image download URL request item
/// </summary>
public class PetImageDownloadUrlRequest
{
    /// <summary>
    /// Pet ID
    /// </summary>
    public Guid PetId { get; set; }

    /// <summary>
    /// Main image file extension (e.g., .jpg, .png)
    /// </summary>
    public string MainImageFileExtension { get; set; } = string.Empty;
}

/// <summary>
/// Request model for getting download presigned URLs for multiple pets
/// </summary>
public class GetPetImageDownloadUrlsRequest
{
    /// <summary>
    /// List of pets with their image file extensions
    /// </summary>
    public List<PetImageDownloadUrlRequest> PetRequests { get; set; } = new();
}

/// <summary>
/// Response model for multiple download presigned URLs
/// </summary>
public class PetImageDownloadUrlsResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Dictionary of pet IDs to their download presigned URLs
    /// </summary>
    public Dictionary<Guid, string?> PetImageUrls { get; set; } = new();

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
