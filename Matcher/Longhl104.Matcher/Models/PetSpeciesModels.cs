namespace Longhl104.Matcher.Models;

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
    /// Presigned download URL for the main image
    /// </summary>
    public string? MainImageDownloadUrl { get; set; }

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
