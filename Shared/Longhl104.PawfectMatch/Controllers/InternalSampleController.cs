using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Longhl104.PawfectMatch.Extensions;

namespace Longhl104.PawfectMatch.Controllers;

/// <summary>
/// Sample controller demonstrating internal-only endpoints for service-to-service communication
/// </summary>
[ApiController]
[Route("api/internal/[controller]")]
public class InternalSampleController : ControllerBase
{
    /// <summary>
    /// Test endpoint for internal service communication only
    /// Requires X-Internal-API-Key header with valid API key
    /// </summary>
    [HttpGet("status")]
    [Authorize(Policy = "InternalOnly")]
    public IActionResult GetInternalStatus()
    {
        var isInternal = HttpContext.IsInternalRequest();
        var authType = HttpContext.GetAuthenticationType();

        return Ok(new
        {
            Message = "Internal service endpoint accessed successfully",
            IsInternalRequest = isInternal,
            AuthenticationType = authType,
            ServiceName = User?.FindFirst("Name")?.Value,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Health check endpoint for internal services
    /// </summary>
    [HttpGet("health")]
    [Authorize(Policy = "InternalOnly")]
    public IActionResult GetInternalHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Internal API",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Example endpoint that can be accessed by both users and internal services
    /// </summary>
    [HttpGet("mixed")]
    [Authorize] // Uses default policy but internal auth can also work
    public IActionResult GetMixedAccess()
    {
        var isInternal = HttpContext.IsInternalRequest();
        var user = HttpContext.GetCurrentUser();
        var authType = HttpContext.GetAuthenticationType();

        return Ok(new
        {
            Message = "Endpoint accessible by both users and internal services",
            IsInternalRequest = isInternal,
            AuthenticationType = authType,
            UserEmail = user?.Email,
            ServiceName = isInternal ? User?.FindFirst("Name")?.Value : null,
            Timestamp = DateTime.UtcNow
        });
    }
}
