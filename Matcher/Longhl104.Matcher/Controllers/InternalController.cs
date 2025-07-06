using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Longhl104.PawfectMatch.Extensions;

namespace Longhl104.Matcher.Controllers;

/// <summary>
/// Example controller demonstrating internal-only endpoints for service communication
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InternalController(ILogger<InternalController> logger) : ControllerBase
{
    private readonly ILogger<InternalController> _logger = logger;

    /// <summary>
    /// Internal endpoint for getting matcher service status
    /// Only accessible by other services with valid API key
    /// </summary>
    [HttpGet("status")]
    [Authorize(Policy = "InternalOnly")]
    public IActionResult GetInternalStatus()
    {
        _logger.LogInformation("Internal status endpoint accessed");

        var isInternal = HttpContext.IsInternalRequest();
        var authType = HttpContext.GetAuthenticationType();

        return Ok(new
        {
            Service = "Matcher",
            Status = "Healthy",
            IsInternalRequest = isInternal,
            AuthenticationType = authType,
            ServiceName = User?.FindFirst("Name")?.Value,
            Timestamp = DateTime.UtcNow,
            Features = new[]
            {
                "Pet Matching",
                "Adopter Preferences",
                "Match Scoring"
            }
        });
    }

    /// <summary>
    /// Internal endpoint for triggering batch matching processes
    /// Only accessible by other services
    /// </summary>
    [HttpPost("batch-match")]
    [Authorize(Policy = "InternalOnly")]
    public IActionResult TriggerBatchMatch([FromBody] BatchMatchRequest request)
    {
        _logger.LogInformation("Batch match triggered internally for shelter: {ShelterId}", request.ShelterId);

        // This would normally trigger actual batch matching logic
        return Ok(new
        {
            Message = "Batch matching process initiated",
            ShelterId = request.ShelterId,
            EstimatedCompletion = DateTime.UtcNow.AddMinutes(30),
            JobId = Guid.NewGuid().ToString()
        });
    }

    /// <summary>
    /// Mixed endpoint that can be accessed by both adopters and internal services
    /// </summary>
    [HttpGet("statistics")]
    [Authorize] // Uses default policy but allows internal access too
    public IActionResult GetStatistics()
    {
        var isInternal = HttpContext.IsInternalRequest();
        var user = HttpContext.GetCurrentUser();

        if (isInternal)
        {
            // Return detailed statistics for internal services
            return Ok(new
            {
                Type = "Internal",
                TotalMatches = 1250,
                ActiveAdopters = 450,
                SuccessfulAdoptions = 89,
                LastUpdated = DateTime.UtcNow,
                DetailedMetrics = new
                {
                    AverageMatchScore = 85.6,
                    MatchesPerDay = 12.3,
                    ConversionRate = 7.12
                }
            });
        }
        else
        {
            // Return basic statistics for regular users
            return Ok(new
            {
                Type = "User",
                UserEmail = user?.Email,
                TotalMatches = 1250,
                RecentMatches = 25,
                LastUpdated = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Request model for batch matching
/// </summary>
public class BatchMatchRequest
{
    public string ShelterId { get; set; } = string.Empty;
    public int? MaxMatches { get; set; }
    public bool IncludeInactive { get; set; } = false;
}
