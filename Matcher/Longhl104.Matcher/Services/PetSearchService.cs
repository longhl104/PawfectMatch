using Longhl104.Matcher.Models;
using Longhl104.PawfectMatch.HttpClient;
using Longhl104.PawfectMatch.Models;
using System.Net;
using System.Text.Json;

namespace Longhl104.Matcher.Services;

public interface IPetSearchService
{
    /// <summary>
    /// Searches for pets by location distance, species, and optionally breed
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>Response containing matching pets sorted by distance</returns>
    Task<PetSearchResponse> SearchPetsAsync(PetSearchRequest request);
}

/// <summary>
/// Service for searching pets by calling ShelterHub APIs
/// </summary>
public class PetSearchService(
    ILogger<PetSearchService> logger,
    IInternalHttpClientFactory httpClientFactory
    ) : IPetSearchService
{
    private readonly ILogger<PetSearchService> _logger = logger;
    private readonly HttpClient _shelterHubHttpClient = httpClientFactory.CreateClient(PawfectMatchServices.ShelterHub);

    /// <summary>
    /// Searches for pets by location distance, species, and optionally breed
    /// </summary>
    /// <param name="request">The search criteria</param>
    /// <returns>Response containing matching pets sorted by distance</returns>
    public async Task<PetSearchResponse> SearchPetsAsync(PetSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Searching pets with criteria: Lat={Latitude}, Lng={Longitude}, MaxDistance={MaxDistance}km, Species={SpeciesId}, Breed={BreedId}",
                request.Latitude, request.Longitude, request.MaxDistanceKm, request.SpeciesId, request.BreedId);

            // Validate request
            var validationError = ValidateSearchRequest(request);
            if (!string.IsNullOrEmpty(validationError))
            {
                return new PetSearchResponse
                {
                    Success = false,
                    ErrorMessage = validationError
                };
            }

            // Build query parameters for ShelterHub internal endpoint
            var queryParams = new List<string>
            {
                $"latitude={request.Latitude}",
                $"longitude={request.Longitude}",
                $"maxDistanceKm={request.MaxDistanceKm}",
                $"pageSize={Math.Min(request.PageSize, 100)}"
            };

            if (request.SpeciesId.HasValue)
            {
                queryParams.Add($"speciesId={request.SpeciesId.Value}");
            }

            if (request.BreedId.HasValue)
            {
                queryParams.Add($"breedId={request.BreedId.Value}");
            }

            if (!string.IsNullOrEmpty(request.NextToken))
            {
                queryParams.Add($"nextToken={Uri.EscapeDataString(request.NextToken)}");
            }

            var queryString = string.Join("&", queryParams);
            var endpoint = $"/api/internal/Pets/search?{queryString}";

            var response = await _shelterHubHttpClient.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to search pets from ShelterHub. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    return new PetSearchResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid search criteria"
                    };
                }

                return new PetSearchResponse
                {
                    Success = false,
                    ErrorMessage = $"Failed to search pets: {response.ReasonPhrase}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<PetSearchResponse>(jsonContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (searchResponse == null)
            {
                _logger.LogError("Failed to deserialize pet search response from ShelterHub");
                return new PetSearchResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to process search results"
                };
            }

            _logger.LogInformation("Successfully retrieved {PetCount} pets from search (Total: {TotalCount})",
                searchResponse.Pets?.Count ?? 0, searchResponse.TotalCount);

            return searchResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while searching pets from ShelterHub");
            return new PetSearchResponse
            {
                Success = false,
                ErrorMessage = "Service temporarily unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while searching pets");
            return new PetSearchResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while searching pets"
            };
        }
    }

    /// <summary>
    /// Validates the pet search request
    /// </summary>
    /// <param name="request">The search request to validate</param>
    /// <returns>Error message if validation fails, empty string if valid</returns>
    private static string ValidateSearchRequest(PetSearchRequest request)
    {
        // Validate latitude (-90 to 90)
        if (request.Latitude < -90 || request.Latitude > 90)
        {
            return "Latitude must be between -90 and 90 degrees";
        }

        // Validate longitude (-180 to 180)
        if (request.Longitude < -180 || request.Longitude > 180)
        {
            return "Longitude must be between -180 and 180 degrees";
        }

        // Validate max distance (1 to 1000 km)
        if (request.MaxDistanceKm < 1 || request.MaxDistanceKm > 1000)
        {
            return "Maximum distance must be between 1 and 1000 kilometers";
        }

        // Validate species ID if provided (must be positive)
        if (request.SpeciesId.HasValue && request.SpeciesId.Value <= 0)
        {
            return "Species ID must be a positive integer";
        }

        // Validate breed ID if provided (must be positive)
        if (request.BreedId.HasValue && request.BreedId.Value <= 0)
        {
            return "Breed ID must be a positive integer";
        }

        // Validate page size (1 to 100)
        if (request.PageSize < 1 || request.PageSize > 100)
        {
            return "Page size must be between 1 and 100";
        }

        return string.Empty;
    }
}
