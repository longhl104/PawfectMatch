using Amazon.S3;
using Amazon.S3.Model;
using Longhl104.ShelterHub.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace Longhl104.ShelterHub.Services;

/// <summary>
/// Cache statistics for download presigned URLs
/// </summary>
public class DownloadUrlCacheStats
{
    /// <summary>
    /// Total number of cached download URLs
    /// </summary>
    public int TotalCachedUrls { get; set; }

    /// <summary>
    /// Cache hit rate (percentage)
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Total number of cache hits
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Total number of cache misses
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Total number of requests
    /// </summary>
    public long TotalRequests { get; set; }
}

/// <summary>
/// Service for handling media uploads to S3
/// </summary>
public interface IMediaService
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
    /// Uses in-memory caching to avoid regenerating URLs for the same S3 object.
    /// Cache expires 5 minutes before the presigned URL expires to ensure valid URLs.
    /// </summary>
    /// <param name="s3Url">The S3 URL to generate a download presigned URL for</param>
    /// <returns>The presigned download URL</returns>
    Task<string?> GenerateDownloadPresignedUrlAsync(string s3Url);

    /// <summary>
    /// Clears the cached presigned download URL for a specific S3 URL
    /// </summary>
    /// <param name="s3Url">The S3 URL to clear from cache</param>
    void ClearDownloadPresignedUrlCache(string s3Url);

    /// <summary>
    /// Gets cache statistics for download presigned URLs
    /// </summary>
    /// <returns>Cache statistics</returns>
    DownloadUrlCacheStats GetDownloadUrlCacheStats();
}


/// <summary>
/// Service for handling media uploads to S3
/// </summary>
public class MediaService(IAmazonS3 s3Client, IConfiguration configuration, IHostEnvironment hostEnvironment, ILogger<MediaService> logger, IMemoryCache memoryCache) : IMediaService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MediaService> _logger = logger;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly string _bucketName = $"pawfectmatch-{hostEnvironment.EnvironmentName.ToLowerInvariant()}-shelter-hub-pet-media";
    private readonly string _bucketRegion = "ap-southeast-2";

    // Cache statistics
    private long _cacheHits = 0;
    private long _cacheMisses = 0;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int PresignedUrlExpirationMinutes = 15;
    private const int CacheExpirationMinutes = 10; // Cache expires 5 minutes before presigned URL expires

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

            // Create cache key for this S3 key
            var cacheKey = $"presigned_download_{key}";

            // Check if presigned URL is already cached
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedUrl))
            {
                Interlocked.Increment(ref _cacheHits);
                _logger.LogInformation("Retrieved cached download presigned URL for key: {Key}", key);
                return cachedUrl;
            }

            Interlocked.Increment(ref _cacheMisses);
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

            // Cache the presigned URL with a shorter expiration than the URL itself
            // This ensures we don't serve expired URLs from cache
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(CacheExpirationMinutes / 2),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(cacheKey, presignedUrl, cacheOptions);

            _logger.LogInformation("Successfully generated and cached download presigned URL for key: {Key}", key);

            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download presigned URL for S3 URL: {S3Url}", s3Url);
            return null;
        }
    }

    /// <summary>
    /// Clears the cached presigned download URL for a specific S3 URL
    /// </summary>
    /// <param name="s3Url">The S3 URL to clear from cache</param>
    public void ClearDownloadPresignedUrlCache(string s3Url)
    {
        try
        {
            if (string.IsNullOrEmpty(s3Url) || !IsValidS3Url(s3Url))
            {
                _logger.LogWarning("Invalid S3 URL provided for cache clearing: {S3Url}", s3Url);
                return;
            }

            // Extract the S3 key from the URL
            var uri = new Uri(s3Url);
            var key = uri.AbsolutePath.TrimStart('/');

            // Create cache key for this S3 key
            var cacheKey = $"presigned_download_{key}";

            // Remove from cache
            _memoryCache.Remove(cacheKey);

            _logger.LogInformation("Cleared cached download presigned URL for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cached download presigned URL for S3 URL: {S3Url}", s3Url);
        }
    }

    /// <summary>
    /// Gets cache statistics for download presigned URLs
    /// </summary>
    /// <returns>Cache statistics</returns>
    public DownloadUrlCacheStats GetDownloadUrlCacheStats()
    {
        var totalRequests = _cacheHits + _cacheMisses;
        var cacheHitRate = totalRequests > 0 ? (double)_cacheHits / totalRequests * 100 : 0;

        return new DownloadUrlCacheStats
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            TotalRequests = totalRequests,
            CacheHitRate = Math.Round(cacheHitRate, 2),
            TotalCachedUrls = GetCachedUrlCount()
        };
    }

    /// <summary>
    /// Gets the count of cached URLs by checking the memory cache
    /// </summary>
    /// <returns>Number of cached URLs</returns>
    private int GetCachedUrlCount()
    {
        try
        {
            // Access the memory cache field to get cache statistics
            // This is a simplified approach - in production, you might want to use a more sophisticated cache monitoring solution
            if (_memoryCache is MemoryCache mc)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(mc) is object coherentState)
                {
                    var propInfo = coherentState.GetType().GetProperty("Count");
                    if (propInfo?.GetValue(coherentState) is int count)
                    {
                        return count;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cached URL count");
        }

        return 0;
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
