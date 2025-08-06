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
