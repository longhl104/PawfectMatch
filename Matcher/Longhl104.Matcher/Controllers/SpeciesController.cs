using Microsoft.AspNetCore.Mvc;
using Longhl104.Matcher.Models;
using Longhl104.Matcher.Services;
using Microsoft.AspNetCore.Authorization;

namespace Longhl104.Matcher.Controllers;

/// <summary>
/// Controller for managing pet species and breeds in the Matcher service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SpeciesController(
    ILogger<SpeciesController> logger,
    ISpeciesService speciesService
) : ControllerBase
{

    /// <summary>
    /// Gets all available pet species
    /// </summary>
    /// <returns>List of all pet species</returns>
    [HttpGet]
    public async Task<ActionResult<GetPetSpeciesResponse>> GetAllSpecies()
    {
        logger.LogInformation("Getting all pet species");

        var result = await speciesService.GetAllSpeciesAsync();

        if (!result.Success)
        {
            return StatusCode(result.ErrorMessage?.Contains("temporarily unavailable") == true ? 503 : 500, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets all breeds for a specific species
    /// </summary>
    /// <param name="speciesId">The species ID</param>
    /// <returns>List of breeds for the species</returns>
    [HttpGet("{speciesId:int}/breeds")]
    public async Task<ActionResult<GetPetBreedsResponse>> GetBreedsBySpeciesId(int speciesId)
    {
        logger.LogInformation("Getting breeds for species ID: {SpeciesId}", speciesId);

        var result = await speciesService.GetBreedsBySpeciesIdAsync(speciesId);

        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
            {
                return BadRequest(result);
            }

            return StatusCode(result.ErrorMessage?.Contains("temporarily unavailable") == true ? 503 : 500, result);
        }

        return Ok(result);
    }
}
