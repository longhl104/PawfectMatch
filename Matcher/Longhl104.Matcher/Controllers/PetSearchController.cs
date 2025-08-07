using Microsoft.AspNetCore.Mvc;
using Longhl104.Matcher.Models;
using Longhl104.Matcher.Services;
using Microsoft.AspNetCore.Authorization;

namespace Longhl104.Matcher.Controllers;

/// <summary>
/// Controller for pet search functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authenticated adopter
public class PetSearchController(
    ILogger<PetSearchController> logger,
    IPetSearchService petSearchService
) : ControllerBase
{
    /// <summary>
    /// Searches for pets by location distance, species, and optionally breed
    /// </summary>
    /// <param name="request">The search criteria including location, species, and optional breed</param>
    /// <returns>List of pets matching the criteria, sorted by distance from user's location</returns>
    [HttpPost("search")]
    [AllowAnonymous]
    public async Task<ActionResult<PetSearchResponse>> SearchPets([FromBody] PetSearchRequest request)
    {
        logger.LogInformation("Pet search requested by user with criteria: Lat={Latitude}, Lng={Longitude}, MaxDistance={MaxDistance}km, Species={SpeciesId}, Breed={BreedId}",
            request?.Latitude, request?.Longitude, request?.MaxDistanceKm, request?.SpeciesId, request?.BreedId);

        if (request == null)
        {
            return BadRequest(new PetSearchResponse
            {
                Success = false,
                ErrorMessage = "Search request is required"
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new PetSearchResponse
            {
                Success = false,
                ErrorMessage = "Invalid search criteria provided"
            });
        }

        var result = await petSearchService.SearchPetsAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
