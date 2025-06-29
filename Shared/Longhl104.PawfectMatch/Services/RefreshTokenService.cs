using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Longhl104.PawfectMatch.Services;

/// <summary>
/// Interface for refresh token management
/// </summary>
public interface IRefreshTokenService
{
    Task StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAt);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeAllUserTokensAsync(string userId);
    Task<string> GetUserIdFromRefreshTokenAsync(string refreshToken);
}

/// <summary>
/// Service for managing refresh tokens in DynamoDB
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IAmazonDynamoDB _dynamoClient;
    private readonly string _tableName;

    public RefreshTokenService()
    {
        _dynamoClient = new AmazonDynamoDBClient();
        var stage = Environment.GetEnvironmentVariable("STAGE") ?? "dev";
        _tableName = $"pawfect-match-refresh-tokens-{stage}";
    }

    public RefreshTokenService(IAmazonDynamoDB dynamoClient, string tableName)
    {
        _dynamoClient = dynamoClient;
        _tableName = tableName;
    }

    /// <summary>
    /// Stores a refresh token in DynamoDB
    /// </summary>
    /// <param name="userId">User ID associated with the token</param>
    /// <param name="refreshToken">The refresh token to store</param>
    /// <param name="expiresAt">When the token expires</param>
    public async Task StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAt)
    {
        try
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["UserId"] = new AttributeValue { S = userId },
                ["RefreshToken"] = new AttributeValue { S = refreshToken },
                ["ExpiresAt"] = new AttributeValue { N = expiresAt.ToFileTimeUtc().ToString() },
                ["CreatedAt"] = new AttributeValue { N = DateTime.UtcNow.ToFileTimeUtc().ToString() },
                ["IsActive"] = new AttributeValue { BOOL = true }
            };

            var request = new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            };

            await _dynamoClient.PutItemAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing refresh token: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates a refresh token by checking if it exists and is still active
    /// </summary>
    /// <param name="userId">User ID associated with the token</param>
    /// <param name="refreshToken">The refresh token to validate</param>
    /// <returns>True if valid and active, false otherwise</returns>
    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                ["UserId"] = new AttributeValue { S = userId },
                ["RefreshToken"] = new AttributeValue { S = refreshToken }
            };

            var request = new GetItemRequest
            {
                TableName = _tableName,
                Key = key
            };

            var response = await _dynamoClient.GetItemAsync(request);

            if (!response.IsItemSet)
                return false;

            // Check if token is active
            if (!response.Item.TryGetValue("IsActive", out AttributeValue? value)
                || !value.BOOL.HasValue
                || !value.BOOL.Value
                )
            {
                return false;
            }

            // Check if token has expired
            if (response.Item.ContainsKey("ExpiresAt"))
            {
                var expiresAt = DateTime.FromFileTimeUtc(long.Parse(response.Item["ExpiresAt"].N));
                if (expiresAt <= DateTime.UtcNow)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating refresh token: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Revokes a specific refresh token by marking it as inactive
    /// </summary>
    /// <param name="userId">User ID associated with the token</param>
    /// <param name="refreshToken">The refresh token to revoke</param>
    public async Task RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        try
        {
            var key = new Dictionary<string, AttributeValue>
            {
                ["UserId"] = new AttributeValue { S = userId },
                ["RefreshToken"] = new AttributeValue { S = refreshToken }
            };

            var request = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = key,
                UpdateExpression = "SET IsActive = :isActive",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":isActive"] = new AttributeValue { BOOL = false }
                }
            };

            await _dynamoClient.UpdateItemAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error revoking refresh token: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">User ID whose tokens should be revoked</param>
    public async Task RevokeAllUserTokensAsync(string userId)
    {
        try
        {
            // Query all refresh tokens for the user
            var queryRequest = new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "UserId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":userId"] = new AttributeValue { S = userId }
                }
            };

            var queryResponse = await _dynamoClient.QueryAsync(queryRequest);

            // Revoke all tokens
            foreach (var item in queryResponse.Items)
            {
                var refreshToken = item["RefreshToken"].S;
                await RevokeRefreshTokenAsync(userId, refreshToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error revoking all user tokens: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetUserIdFromRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "RefreshToken = :refreshToken",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":refreshToken"] = new AttributeValue { S = refreshToken }
                }
            };

            var scanResponse = await _dynamoClient.ScanAsync(scanRequest);

            if (scanResponse.Items.Count == 0)
                return string.Empty;

            // Assuming only one token per user
            return scanResponse.Items[0]["UserId"].S;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user ID from refresh token: {ex.Message}");
            throw;
        }
    }
}
