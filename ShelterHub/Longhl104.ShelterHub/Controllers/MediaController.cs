using Microsoft.AspNetCore.Mvc;
using Longhl104.ShelterHub.Services;

namespace Longhl104.ShelterHub.Controllers;

/// <summary>
/// Controller for handling media upload operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MediaController(IMediaService mediaUploadService) : ControllerBase
{
    /// <summary>
    /// Gets cache statistics for download presigned URLs
    /// </summary>
    /// <returns>Cache statistics</returns>
    [HttpGet("cache-stats")]
    public ActionResult<DownloadUrlCacheStats> GetCacheStats()
    {
        var stats = mediaUploadService.GetDownloadUrlCacheStats();
        return Ok(stats);
    }

    /// <summary>
    /// Clears the cached presigned download URL for a specific S3 URL
    /// </summary>
    /// <param name="s3Url">The S3 URL to clear from cache</param>
    /// <returns>Success message</returns>
    [HttpDelete("cache/{s3Url}")]
    public ActionResult ClearCache(string s3Url)
    {
        s3Url = Uri.UnescapeDataString(s3Url); // Decode URL if necessary
        if (string.IsNullOrEmpty(s3Url))
        {
            return BadRequest(new { error = "S3 URL is required" });
        }

        mediaUploadService.ClearDownloadPresignedUrlCache(s3Url);
        return Ok(new { message = "Cache cleared successfully", s3Url });
    }
}
