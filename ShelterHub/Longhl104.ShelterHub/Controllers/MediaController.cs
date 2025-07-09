using Microsoft.AspNetCore.Mvc;
using Longhl104.ShelterHub.Models;
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
    /// Generates a presigned URL for uploading pet images to S3
    /// </summary>
    /// <param name="request">The presigned URL request</param>
    /// <returns>Presigned URL for upload</returns>
    [HttpPost("presigned-url")]
    public async Task<ActionResult<PresignedUrlResponse>> GeneratePresignedUrl([FromBody] PresignedUrlRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await mediaUploadService.GeneratePresignedUrlAsync(request);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    /// <summary>
    /// Validates if an S3 URL is valid and belongs to our bucket
    /// </summary>
    /// <param name="url">The S3 URL to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-url")]
    public ActionResult<object> ValidateS3Url([FromBody] string url)
    {
        var isValid = mediaUploadService.IsValidS3Url(url);

        return Ok(new { isValid, url });
    }

    /// <summary>
    /// Generates a presigned download URL for an S3 image
    /// </summary>
    /// <param name="request">The download request containing the S3 URL</param>
    /// <returns>Presigned download URL</returns>
    [HttpGet("presigned-url/{s3Url}")]
    public async Task<ActionResult<string>> GenerateDownloadPresignedUrl(string s3Url)
    {
        s3Url = Uri.UnescapeDataString(s3Url); // Decode URL if necessary
        if (string.IsNullOrEmpty(s3Url))
        {
            return BadRequest(new { error = "S3 URL is required" });
        }

        var presignedUrl = await mediaUploadService.GenerateDownloadPresignedUrlAsync(s3Url);

        if (string.IsNullOrEmpty(presignedUrl))
        {
            return BadRequest(new { error = "Failed to generate presigned download URL" });
        }

        return Ok(presignedUrl);
    }

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
