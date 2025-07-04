using Microsoft.AspNetCore.Mvc;
using Longhl104.PawfectMatch.Models.Identity;

namespace Longhl104.Matcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        if (HttpContext.Items["User"] is not UserProfile user)
        {
            logger.LogWarning("User not found in context.");
            return Unauthorized(new { Message = "User not authenticated." });
        }

        logger.LogInformation("Returning current user: {UserId}", user.UserId);
        return Ok(user);
    }
}
