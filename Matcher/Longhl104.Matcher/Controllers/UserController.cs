using Microsoft.AspNetCore.Mvc;
using Longhl104.Matcher.Extensions;

namespace Longhl104.Matcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(ILogger<UserController> logger) : ControllerBase
{
    /// <summary>
    /// Get current user profile (requires authentication)
    /// </summary>
    [HttpGet("profile")]
    public IActionResult GetUserProfile()
    {
        var user = HttpContext.GetCurrentUser();

        if (user == null)
        {
            logger.LogWarning("User profile requested but no user found in context");
            return Unauthorized(new { message = "User not authenticated" });
        }

        logger.LogInformation("User profile requested for: {Email}", user.Email);

        return Ok(new
        {
            success = true,
            message = "User profile retrieved successfully",
            data = user
        });
    }

    /// <summary>
    /// Get current user ID (requires authentication)
    /// </summary>
    [HttpGet("id")]
    public IActionResult GetUserId()
    {
        var userId = HttpContext.GetCurrentUserId();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        return Ok(new
        {
            success = true,
            data = new { userId }
        });
    }
}
