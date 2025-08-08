using System.ComponentModel;
using System.Text.Json.Serialization;
using Longhl104.ShelterHub.Models.PostgreSql;
using Longhl104.PawfectMatch.Extensions;

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
    public required Guid PetId { get; set; }

    /// <summary>
    /// Weight of the pet in pounds
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Color/markings of the pet
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Special needs or medical conditions
    /// </summary>
    public string SpecialNeeds { get; set; } = string.Empty;

    public required int PetPostgreSqlId { get; set; }

    public string? MainImageFileExtension { get; set; }

    public string? Description { get; set; }

    public PetStatus? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public decimal? AdoptionFee { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Name { get; set; }

    public bool? IsSpayedNeutered { get; set; }

    public bool? IsHouseTrained { get; set; }

    public bool? IsGoodWithKids { get; set; }

    public bool? IsGoodWithPets { get; set; }

    public bool? IsVaccinated { get; set; }

    public bool? IsMicrochipped { get; set; }

    public int? SpeciesId { get; set; }

    public int? BreedId { get; set; }

    /// <summary>
    /// Species name (e.g., Dog, Cat, etc.) - populated from PostgreSQL
    /// </summary>
    public string? Species { get; set; }

    /// <summary>
    /// Breed name - populated from PostgreSQL
    /// </summary>
    public string? Breed { get; set; }

    public Guid? ShelterId { get; set; }
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
    public required int SpeciesId { get; set; }

    /// <summary>
    /// Breed of the pet
    /// </summary>
    public required int BreedId { get; set; }

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

    /// <summary>
    /// Adoption fee for the pet
    /// </summary>
    public decimal AdoptionFee { get; set; } = 0;

    /// <summary>
    /// Weight of the pet in pounds
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Color/markings of the pet
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pet is spayed/neutered
    /// </summary>
    public bool IsSpayedNeutered { get; set; } = false;

    /// <summary>
    /// Whether the pet is vaccinated
    /// </summary>
    public bool IsVaccinated { get; set; } = false;

    /// <summary>
    /// Whether the pet is microchipped
    /// </summary>
    public bool IsMicrochipped { get; set; } = false;

    /// <summary>
    /// Whether the pet is house trained
    /// </summary>
    public bool IsHouseTrained { get; set; } = false;

    /// <summary>
    /// Whether the pet is good with kids
    /// </summary>
    public bool IsGoodWithKids { get; set; } = false;

    /// <summary>
    /// Whether the pet is good with other pets
    /// </summary>
    public bool IsGoodWithPets { get; set; } = false;

    /// <summary>
    /// Special needs or medical conditions
    /// </summary>
    public string SpecialNeeds { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the pet
    /// </summary>
    public PetStatus Status { get; set; } = PetStatus.Available;
}

/// <summary>
/// Request model for updating an existing pet
/// </summary>
public class UpdatePetRequest
{
    /// <summary>
    /// Name of the pet
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Species of the pet (Dog, Cat, etc.)
    /// </summary>
    public required int SpeciesId { get; set; }

    /// <summary>
    /// Breed of the pet
    /// </summary>
    public required int BreedId { get; set; }

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

    /// <summary>
    /// Adoption fee for the pet
    /// </summary>
    public decimal AdoptionFee { get; set; } = 0;

    /// <summary>
    /// Weight of the pet in pounds
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Color/markings of the pet
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pet is spayed/neutered
    /// </summary>
    public bool IsSpayedNeutered { get; set; } = false;

    /// <summary>
    /// Whether the pet is vaccinated
    /// </summary>
    public bool IsVaccinated { get; set; } = false;

    /// <summary>
    /// Whether the pet is microchipped
    /// </summary>
    public bool IsMicrochipped { get; set; } = false;

    /// <summary>
    /// Whether the pet is house trained
    /// </summary>
    public bool IsHouseTrained { get; set; } = false;

    /// <summary>
    /// Whether the pet is good with kids
    /// </summary>
    public bool IsGoodWithKids { get; set; } = false;

    /// <summary>
    /// Whether the pet is good with other pets
    /// </summary>
    public bool IsGoodWithPets { get; set; } = false;

    /// <summary>
    /// Special needs or medical conditions
    /// </summary>
    public string SpecialNeeds { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the pet
    /// </summary>
    public PetStatus Status { get; set; } = PetStatus.Available;
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

/// <summary>
/// Media file type enumeration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaFileType
{
    [AmbientValue("Image")]
    Image,
    [AmbientValue("Video")]
    Video,
    [AmbientValue("Document")]
    Document
}

/// <summary>
/// Pet media file model
/// </summary>
public class PetMediaFile
{
    /// <summary>
    /// Unique identifier for the media file
    /// </summary>
    public Guid MediaFileId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Pet ID this media belongs to
    /// </summary>
    public Guid PetId { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension (e.g., .jpg, .mp4, .pdf)
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Type of media file
    /// </summary>
    public MediaFileType? FileType { get; set; }

    /// <summary>
    /// Content type/MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// S3 key/path for the file
    /// </summary>
    public string S3Key { get; set; } = string.Empty;

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Display order for the media file
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Request model for uploading multiple media files
/// </summary>
public class UploadMediaFilesRequest
{
    /// <summary>
    /// Pet ID to associate media files with
    /// </summary>
    public Guid PetId { get; set; }

    /// <summary>
    /// List of media file upload requests
    /// </summary>
    public List<MediaFileUploadRequest> MediaFiles { get; set; } = new();
}

/// <summary>
/// Individual media file upload request
/// </summary>
public class MediaFileUploadRequest
{
    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type/MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Type of media file
    /// </summary>
    public MediaFileType FileType { get; set; }
}

/// <summary>
/// Response model for media file upload URLs
/// </summary>
public class MediaFileUploadResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of upload URL responses
    /// </summary>
    public List<MediaFileUploadUrlResponse> UploadUrls { get; set; } = new();

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Individual media file upload URL response
/// </summary>
public class MediaFileUploadUrlResponse
{
    /// <summary>
    /// Media file ID
    /// </summary>
    public Guid MediaFileId { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Presigned upload URL
    /// </summary>
    public string PresignedUrl { get; set; } = string.Empty;

    /// <summary>
    /// S3 key for the file
    /// </summary>
    public string S3Key { get; set; } = string.Empty;

    /// <summary>
    /// When the presigned URL expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Response model for getting pet media files
/// </summary>
public class GetPetMediaResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of images
    /// </summary>
    public List<PetMediaFileResponse> Images { get; set; } = new();

    /// <summary>
    /// List of videos
    /// </summary>
    public List<PetMediaFileResponse> Videos { get; set; } = new();

    /// <summary>
    /// List of documents
    /// </summary>
    public List<PetMediaFileResponse> Documents { get; set; } = new();

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Pet media file response model with download URL
/// </summary>
public class PetMediaFileResponse
{
    /// <summary>
    /// Media file ID
    /// </summary>
    public Guid MediaFileId { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Type of media file
    /// </summary>
    public MediaFileType? FileType { get; set; }

    /// <summary>
    /// Content type/MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Presigned download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Display order for the media file
    /// </summary>
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Request model for deleting media files
/// </summary>
public class DeleteMediaFilesRequest
{
    /// <summary>
    /// List of media file IDs to delete
    /// </summary>
    public List<Guid> MediaFileIds { get; set; } = new();
}

/// <summary>
/// Response model for deleting media files
/// </summary>
public class DeleteMediaFilesResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of files successfully deleted
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// List of media file IDs that failed to delete
    /// </summary>
    public List<Guid> FailedDeletes { get; set; } = new();

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response model for getting all pet species
/// </summary>
public class GetPetSpeciesResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of pet species
    /// </summary>
    public List<PetSpeciesDto> Species { get; set; } = [];

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Data transfer object for pet species
/// </summary>
public class PetSpeciesDto
{
    /// <summary>
    /// Species ID
    /// </summary>
    public int SpeciesId { get; set; }

    /// <summary>
    /// Species name
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response model for getting breeds by species ID
/// </summary>
public class GetPetBreedsResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of pet breeds for the species
    /// </summary>
    public List<PetBreedDto> Breeds { get; set; } = [];

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Data transfer object for pet breed
/// </summary>
public class PetBreedDto
{
    /// <summary>
    /// Breed ID
    /// </summary>
    public int BreedId { get; set; }

    /// <summary>
    /// Breed name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Species ID that this breed belongs to
    /// </summary>
    public int SpeciesId { get; set; }
}

/// <summary>
/// Request model for searching pets by location and criteria
/// </summary>
public class PetSearchRequest
{
    /// <summary>
    /// User's latitude for location-based search
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// User's longitude for location-based search
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// Maximum distance in kilometers from user's location
    /// </summary>
    public decimal MaxDistanceKm { get; set; } = 50;

    /// <summary>
    /// Species ID to filter by (optional - null for all species)
    /// </summary>
    public int? SpeciesId { get; set; }

    /// <summary>
    /// Optional breed ID to filter by
    /// </summary>
    public int? BreedId { get; set; }

    /// <summary>
    /// Number of items per page (default: 10, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Token for pagination
    /// </summary>
    public string? NextToken { get; set; }
}

/// <summary>
/// Data transfer object for pet in search results
/// </summary>
public class PetSearchResultDto
{
    /// <summary>
    /// Pet ID from PostgreSQL
    /// </summary>
    public int PetPostgreSqlId { get; set; }

    /// <summary>
    /// Pet name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Pet species name
    /// </summary>
    public string? Species { get; set; }

    /// <summary>
    /// Pet breed name
    /// </summary>
    public string? Breed { get; set; }

    /// <summary>
    /// Pet age in months
    /// </summary>
    public int? AgeInMonths { get; set; }

    /// <summary>
    /// Pet gender
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Pet description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Adoption fee
    /// </summary>
    public decimal? AdoptionFee { get; set; }

    /// <summary>
    /// Main image file extension for constructing image URL
    /// </summary>
    public string? MainImageFileExtension { get; set; }

    /// <summary>
    /// Shelter information
    /// </summary>
    public PetSearchShelterDto? Shelter { get; set; }

    /// <summary>
    /// Distance from user's location in kilometers
    /// </summary>
    public decimal DistanceKm { get; set; }
}

/// <summary>
/// Data transfer object for shelter in pet search results
/// </summary>
public class PetSearchShelterDto
{
    /// <summary>
    /// Shelter ID
    /// </summary>
    public Guid ShelterId { get; set; }

    /// <summary>
    /// Shelter name
    /// </summary>
    public string? ShelterName { get; set; }

    /// <summary>
    /// Shelter address
    /// </summary>
    public string? ShelterAddress { get; set; }

    /// <summary>
    /// Shelter contact number
    /// </summary>
    public string? ShelterContactNumber { get; set; }

    /// <summary>
    /// Shelter latitude
    /// </summary>
    public decimal? ShelterLatitude { get; set; }

    /// <summary>
    /// Shelter longitude
    /// </summary>
    public decimal? ShelterLongitude { get; set; }
}

/// <summary>
/// Response model for pet search
/// </summary>
public class PetSearchResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of pets matching the search criteria
    /// </summary>
    public List<PetSearchResultDto> Pets { get; set; } = [];

    /// <summary>
    /// Total count of pets matching the criteria (for pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Token for next page (if available)
    /// </summary>
    public string? NextToken { get; set; }

    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Query result class for raw SQL pet search query
/// </summary>
public class PetSearchQueryResult
{
    public int PetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Description { get; set; }
    public decimal? AdoptionFee { get; set; }
    public string? MainImageFileExtension { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public int ShelterId { get; set; }
    public string? ShelterName { get; set; }
    public string? ShelterAddress { get; set; }
    public string? ShelterContactNumber { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SpeciesName { get; set; }
    public string? BreedName { get; set; }
    public double DistanceKm { get; set; }
}

/// <summary>
/// Result class for SQL COUNT queries
/// </summary>
public class CountResult
{
    public int Value { get; set; }
}
