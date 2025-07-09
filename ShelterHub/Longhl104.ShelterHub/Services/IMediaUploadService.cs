using Longhl104.ShelterHub.Models;

namespace Longhl104.ShelterHub.Services;

/// <summary>
/// Service for handling media uploads to S3
/// </summary>
public interface IMediaUploadService
{
    /// <summary>
    /// Generates a presigned URL for uploading an image to S3
    /// </summary>
    /// <param name="request">The presigned URL request</param>
    /// <returns>The presigned URL response</returns>
    Task<PresignedUrlResponse> GeneratePresignedUrlAsync(PresignedUrlRequest request);

    /// <summary>
    /// Validates if an S3 URL is valid and belongs to our bucket
    /// </summary>
    /// <param name="url">The S3 URL to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidS3Url(string url);

    /// <summary>
    /// Generates a presigned URL for downloading an image from S3
    /// </summary>
    /// <param name="s3Url">The S3 URL to generate a download presigned URL for</param>
    /// <returns>The presigned download URL</returns>
    Task<string?> GenerateDownloadPresignedUrlAsync(string s3Url);
}
