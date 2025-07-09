using Amazon.S3;
using Amazon.S3.Model;
using Longhl104.ShelterHub.Models;
using System.Linq;
using System.Text.RegularExpressions;

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


/// <summary>
/// Service for handling media uploads to S3
/// </summary>
public class MediaUploadService(IAmazonS3 s3Client, IConfiguration configuration, IHostEnvironment hostEnvironment, ILogger<MediaUploadService> logger) : IMediaUploadService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MediaUploadService> _logger = logger;
    private readonly string _bucketName = $"pawfectmatch-{hostEnvironment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pet-media";
    private readonly string _bucketRegion = "ap-southeast-2";

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private const int PresignedUrlExpirationMinutes = 15;

    /// <summary>
    /// Generates a presigned URL for uploading an image to S3
    /// </summary>
    /// <param name="request">The presigned URL request</param>
    /// <returns>The presigned URL response</returns>
    public async Task<PresignedUrlResponse> GeneratePresignedUrlAsync(PresignedUrlRequest request)
    {
        try
        {
            _logger.LogInformation("Generating presigned URL for file: {FileName}, ContentType: {ContentType}, Size: {Size}",
                request.FileName, request.ContentType, request.FileSizeBytes);

            // Validate request
            var validationResult = ValidateUploadRequest(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for presigned URL request: {ErrorMessage}", validationResult.ErrorMessage);
                return new PresignedUrlResponse
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            // Generate unique key for the file
            var fileExtension = Path.GetExtension(request.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = $"pets/{request.ShelterId}/{uniqueFileName}";

            _logger.LogInformation("Generated S3 key: {Key} for bucket: {BucketName}", key, _bucketName);

            // Create presigned URL request
            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(PresignedUrlExpirationMinutes),
                ContentType = request.ContentType
            };

            // Generate the presigned URL
            var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);

            _logger.LogInformation("Successfully generated presigned URL for key: {Key}", key);

            // Generate the final S3 URL (what the file will be accessible at after upload)
            var s3Url = $"https://{_bucketName}.s3.{_bucketRegion}.amazonaws.com/{key}";

            return new PresignedUrlResponse
            {
                Success = true,
                PresignedUrl = presignedUrl,
                S3Url = s3Url,
                Key = key,
                ExpiresAt = presignedRequest.Expires
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for file: {FileName}", request.FileName);
            return new PresignedUrlResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate presigned URL: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates if an S3 URL is valid and belongs to our bucket
    /// </summary>
    /// <param name="url">The S3 URL to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidS3Url(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            // Check if URL matches our bucket pattern
            var bucketPattern = $@"^https://{Regex.Escape(_bucketName)}\.s3\.{Regex.Escape(_bucketRegion)}\.amazonaws\.com/pets/[\w-]+/[\w-]+\.(jpg|jpeg|png|gif|webp)$";
            return Regex.IsMatch(url, bucketPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a presigned URL for downloading an image from S3
    /// </summary>
    /// <param name="s3Url">The S3 URL to generate a download presigned URL for</param>
    /// <returns>The presigned download URL</returns>
    public async Task<string?> GenerateDownloadPresignedUrlAsync(string s3Url)
    {
        try
        {
            if (string.IsNullOrEmpty(s3Url) || !IsValidS3Url(s3Url))
            {
                _logger.LogWarning("Invalid S3 URL provided for download presigned URL generation: {S3Url}", s3Url);
                return null;
            }

            // Extract the S3 key from the URL
            var uri = new Uri(s3Url);
            var key = uri.AbsolutePath.TrimStart('/');

            _logger.LogInformation("Generating download presigned URL for key: {Key}", key);

            // Create presigned URL request for download
            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(PresignedUrlExpirationMinutes)
            };

            var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);

            _logger.LogInformation("Successfully generated download presigned URL for key: {Key}", key);

            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download presigned URL for S3 URL: {S3Url}", s3Url);
            return null;
        }
    }

    private static ValidationResult ValidateUploadRequest(PresignedUrlRequest request)
    {
        // Check file name
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "File name is required" };
        }

        // Check file extension
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File extension {extension} is not allowed. Allowed extensions: {string.Join(", ", AllowedExtensions)}"
            };
        }

        // Check content type
        if (string.IsNullOrWhiteSpace(request.ContentType) || !AllowedMimeTypes.Contains(request.ContentType.ToLowerInvariant()))
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Content type {request.ContentType} is not allowed. Allowed types: {string.Join(", ", AllowedMimeTypes)}"
            };
        }

        // Check file size
        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxFileSizeBytes)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File size must be between 1 byte and {MaxFileSizeBytes / (1024 * 1024)}MB"
            };
        }

        // Check shelter ID
        if (request.ShelterId == Guid.Empty)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "Shelter ID is required" };
        }

        return new ValidationResult { IsValid = true };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
