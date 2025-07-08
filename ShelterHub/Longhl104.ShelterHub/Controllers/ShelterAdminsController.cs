using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Longhl104.ShelterHub.Models;
using Longhl104.ShelterHub.Services;
using Longhl104.PawfectMatch.Extensions;

namespace Longhl104.ShelterHub.Controllers;

/// <summary>
/// Controller for managing shelter admin profiles
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShelterAdminsController(
    IShelterService _shelterService,
    ILogger<ShelterAdminsController> _logger
    ) : ControllerBase
{
    /// <summary>
    /// Creates a new shelter admin profile and associated shelter
    /// This endpoint is used by the Identity service during registration
    /// </summary>
    /// <param name="request">The shelter admin creation request</param>
    /// <returns>Response indicating success or failure</returns>
    [HttpPost]
    [Route("~/api/internal/[controller]")]
    [Authorize(Policy = "InternalOnly")] // Only allow internal service calls
    public async Task<ActionResult<ShelterAdminResponse>> CreateShelterAdmin([FromBody] CreateShelterAdminRequest request)
    {
        try
        {
            _logger.LogInformation("Creating shelter admin for UserId: {UserId}", request.UserId);

            var response = await _shelterService.CreateShelterAdminAsync(request);

            if (response.Success)
            {
                _logger.LogInformation("Successfully created shelter admin for UserId: {UserId}, ShelterId: {ShelterId}",
                    request.UserId, response.ShelterId);
                return Ok(response);
            }

            _logger.LogWarning("Failed to create shelter admin for UserId: {UserId}. Reason: {Message}",
                request.UserId, response.Message);
            return BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating shelter admin for UserId: {UserId}", request.UserId);
            return StatusCode(500, new ShelterAdminResponse
            {
                Success = false,
                Message = "An unexpected error occurred while creating the shelter admin profile",
                UserId = request.UserId
            });
        }
    }

    /// <summary>
    /// Gets the current shelter admin's profile
    /// </summary>
    /// <returns>The shelter admin profile</returns>
    [HttpGet("profile")]
    [Authorize] // Requires authenticated shelter admin
    public async Task<ActionResult<ShelterAdmin>> GetProfile()
    {
        try
        {
            var user = HttpContext.GetCurrentUser();
            if (user is null)
            {
                _logger.LogWarning("User not found in context");
                return Unauthorized(new { Message = "User not authenticated" });
            }

            _logger.LogInformation("Getting shelter admin profile for UserId: {UserId}", user.UserId);

            var shelterAdmin = await _shelterService.GetShelterAdminAsync(user.UserId);

            if (shelterAdmin == null)
            {
                _logger.LogWarning("Shelter admin profile not found for UserId: {UserId}", user.UserId);
                return NotFound(new { Message = "Shelter admin profile not found" });
            }

            return Ok(shelterAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shelter admin profile");
            return StatusCode(500, new { Message = "An error occurred while retrieving the profile" });
        }
    }

    /// <summary>
    /// Gets a shelter admin profile by user ID (internal use)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The shelter admin profile</returns>
    [HttpGet("{userId}")]
    [Authorize(Policy = "InternalOnly")] // Only allow internal service calls
    public async Task<ActionResult<ShelterAdmin>> GetShelterAdmin(Guid userId)
    {
        try
        {
            _logger.LogInformation("Getting shelter admin for UserId: {UserId}", userId);

            var shelterAdmin = await _shelterService.GetShelterAdminAsync(userId);

            if (shelterAdmin == null)
            {
                _logger.LogWarning("Shelter admin not found for UserId: {UserId}", userId);
                return NotFound(new { Message = "Shelter admin not found" });
            }

            return Ok(shelterAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shelter admin for UserId: {UserId}", userId);
            return StatusCode(500, new { Message = "An error occurred while retrieving the shelter admin" });
        }
    }
}
