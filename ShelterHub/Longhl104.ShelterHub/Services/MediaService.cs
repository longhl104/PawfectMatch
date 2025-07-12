using Amazon.S3;
using Amazon.S3.Model;
using Longhl104.ShelterHub.Models;
using Microsoft.Extensions.Caching.Memory;

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
    /// Generates a presigned URL for downloading an image from S3
    /// Uses in-memory caching to avoid regenerating URLs for the same S3 object.
    /// Cache expires 5 minutes before the presigned URL expires to ensure valid URLs.
    /// </summary>
    /// <param name="s3Url">The S3 URL to generate a download presigned URL for</param>
    /// <returns>The presigned download URL</returns>
    Task<string?> GenerateDownloadPresignedUrlAsync(string bucketName, string s3Url);

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
public class MediaService(
    IAmazonS3 s3Client,
    ILogger<MediaService> logger,
    IMemoryCache memoryCache
    ) : IMediaService
{
    private readonly IAmazonS3 _s3Client = s3Client;
    private readonly ILogger<MediaService> _logger = logger;
    private readonly IMemoryCache _memoryCache = memoryCache;

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
            _logger.LogInformation("Generating presigned URL for file key: {Key}, ContentType: {ContentType}, Size: {Size}",
                request.Key, request.ContentType, request.FileSizeBytes);

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
            var fileExtension = Path.GetExtension(request.Key).ToLowerInvariant();

            // Create presigned URL request
            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = request.BucketName,
                Key = request.Key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(PresignedUrlExpirationMinutes),
                ContentType = request.ContentType
            };

            // Generate the presigned URL
            var presignedUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);

            _logger.LogInformation("Successfully generated presigned URL for key: {Key}", request.Key);

            return new PresignedUrlResponse
            {
                Success = true,
                PresignedUrl = presignedUrl,
                Key = request.Key,
                ExpiresAt = presignedRequest.Expires
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for file key: {Key}", request.Key);
            return new PresignedUrlResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate presigned URL: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Generates a presigned URL for downloading an image from S3
    /// </summary>
    /// <param name="key">The S3 URL to generate a download presigned URL for</param>
    /// <returns>The presigned download URL</returns>
    public async Task<string?> GenerateDownloadPresignedUrlAsync(string bucketName, string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Invalid S3 key provided for download presigned URL generation: {S3Key}", key);
                return null;
            }

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
                BucketName = bucketName,
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
            _logger.LogError(ex, "Failed to generate download presigned URL for S3 key: {S3Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Clears the cached presigned download URL for a specific S3 URL
    /// </summary>
    /// <param name="key">The S3 URL to clear from cache</param>
    public void ClearDownloadPresignedUrlCache(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning("Invalid S3 key provided for cache clearing: {S3Key}", key);
                return;
            }

            // Create cache key for this S3 key
            var cacheKey = $"presigned_download_{key}";

            // Remove from cache
            _memoryCache.Remove(cacheKey);

            _logger.LogInformation("Cleared cached download presigned URL for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cached download presigned URL for S3 key: {S3Key}", key);
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
        // Check file extension
        var extension = Path.GetExtension(request.Key).ToLowerInvariant();
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

        return new ValidationResult { IsValid = true };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
