using Longhl104.Matcher.Models;
using Longhl104.PawfectMatch.HttpClient;
using Longhl104.PawfectMatch.Models;
using System.Net;

namespace Longhl104.Matcher.Services;

public interface ISpeciesService
{
    /// <summary>
    /// Gets all available pet species
    /// </summary>
    /// <returns>Response containing all pet species</returns>
    Task<GetPetSpeciesResponse> GetAllSpeciesAsync();

    /// <summary>
    /// Gets all breeds for a specific species
    /// </summary>
    /// <param name="speciesId">The species ID</param>
    /// <returns>Response containing breeds for the species</returns>
    Task<GetPetBreedsResponse> GetBreedsBySpeciesIdAsync(int speciesId);
}


/// <summary>
/// Service for managing pet species and breeds by calling ShelterHub APIs
/// </summary>
public class SpeciesService(
    ILogger<SpeciesService> logger,
    IInternalHttpClientFactory httpClientFactory
    ) : ISpeciesService
{
    private readonly ILogger<SpeciesService> _logger = logger;
    private readonly HttpClient _shelterHubHttpClient = httpClientFactory.CreateClient(PawfectMatchServices.ShelterHub);

    /// <summary>
    /// Gets all available pet species from ShelterHub
    /// </summary>
    /// <returns>Response containing all pet species</returns>
    public async Task<GetPetSpeciesResponse> GetAllSpeciesAsync()
    {
        try
        {
            _logger.LogInformation("Getting all pet species from ShelterHub");

            var response = await _shelterHubHttpClient.GetAsync("/api/Pets/species");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get species from ShelterHub. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                return new GetPetSpeciesResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to retrieve species: {response.ReasonPhrase}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var speciesResponse = System.Text.Json.JsonSerializer.Deserialize<GetPetSpeciesResponse>(jsonContent,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (speciesResponse == null)
            {
                _logger.LogError("Failed to deserialize species response from ShelterHub");
                return new GetPetSpeciesResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to process species data"
                };
            }

            _logger.LogInformation("Successfully retrieved {SpeciesCount} species from ShelterHub",
                speciesResponse.Species?.Count ?? 0);

            return speciesResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while getting species from ShelterHub");
            return new GetPetSpeciesResponse
            {
                Success = false,
                ErrorMessage = "Service temporarily unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting species");
            return new GetPetSpeciesResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving species"
            };
        }
    }

    /// <summary>
    /// Gets all breeds for a specific species from ShelterHub
    /// </summary>
    /// <param name="speciesId">The species ID</param>
    /// <returns>Response containing breeds for the species</returns>
    public async Task<GetPetBreedsResponse> GetBreedsBySpeciesIdAsync(int speciesId)
    {
        try
        {
            _logger.LogInformation("Getting breeds for species ID: {SpeciesId} from ShelterHub", speciesId);

            var response = await _shelterHubHttpClient.GetAsync($"/api/Pets/species/{speciesId}/breeds");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get breeds from ShelterHub for species {SpeciesId}. Status: {StatusCode}, Error: {Error}",
                    speciesId, response.StatusCode, errorContent);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    // ShelterHub returns BadRequest when species doesn't exist
                    return new GetPetBreedsResponse
                    {
                        Success = false,
                        ErrorMessage = $"Species with ID {speciesId} not found"
                    };
                }

                return new GetPetBreedsResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to retrieve breeds: {response.ReasonPhrase}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var breedsResponse = System.Text.Json.JsonSerializer.Deserialize<GetPetBreedsResponse>(jsonContent,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (breedsResponse == null)
            {
                _logger.LogError("Failed to deserialize breeds response from ShelterHub for species {SpeciesId}", speciesId);
                return new GetPetBreedsResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to process breeds data"
                };
            }

            _logger.LogInformation("Successfully retrieved {BreedCount} breeds for species {SpeciesId} from ShelterHub",
                breedsResponse.Breeds?.Count ?? 0, speciesId);

            return breedsResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while getting breeds for species {SpeciesId} from ShelterHub", speciesId);
            return new GetPetBreedsResponse
            {
                Success = false,
                ErrorMessage = "Service temporarily unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting breeds for species {SpeciesId}", speciesId);
            return new GetPetBreedsResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while retrieving breeds"
            };
        }
    }
}
