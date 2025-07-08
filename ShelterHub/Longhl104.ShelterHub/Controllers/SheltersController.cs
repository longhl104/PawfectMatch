using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Longhl104.ShelterHub.Models;
using Longhl104.ShelterHub.Services;
using Longhl104.PawfectMatch.Extensions;

namespace Longhl104.ShelterHub.Controllers;

/// <summary>
/// Controller for managing shelters
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authenticated shelter admin
public class SheltersController(
    IShelterService _shelterService,
    ILogger<SheltersController> _logger
    ) : ControllerBase
{
    /// <summary>
    /// Queries the shelter associated with the current shelter admin with specific attributes
    /// </summary>
    /// <param name="request">The query request containing attributes to retrieve</param>
    /// <returns>The shelter information with only the requested attributes</returns>
    [HttpPost("my-shelter/query")]
    public async Task<ActionResult<Shelter>> QueryMyShelter([FromBody] QueryShelterRequest request)
    {
        try
        {
            var user = HttpContext.GetCurrentUser();
            if (user is null)
            {
                _logger.LogWarning("User not found in context");
                return Unauthorized(new { Message = "User not authenticated" });
            }

            _logger.LogInformation("Querying shelter for shelter admin UserId: {UserId} with attributes: {Attributes}",
                user.UserId, request?.AttributesToGet != null ? string.Join(", ", request.AttributesToGet) : "all");

            // First get the shelter admin to find the shelter ID
            var shelterAdmin = await _shelterService.GetShelterAdminAsync(
                user.UserId,
                ["ShelterId"]
                );

            if (shelterAdmin == null)
            {
                _logger.LogWarning("Shelter admin profile not found for UserId: {UserId}", user.UserId);
                return NotFound(new { Message = "Shelter admin profile not found" });
            }

            // Get the shelter information with specified attributes
            var shelter = await _shelterService.GetShelterAsync(
                shelterAdmin.ShelterId,
                request?.AttributesToGet
                );

            if (shelter == null)
            {
                _logger.LogWarning("Shelter not found for ShelterId: {ShelterId}", shelterAdmin.ShelterId);
                return NotFound(new { Message = "Shelter not found" });
            }

            return Ok(shelter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying shelter for current user");
            return StatusCode(500, new { Message = "An error occurred while retrieving the shelter information" });
        }
    }
}
