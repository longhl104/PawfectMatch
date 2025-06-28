using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace RefreshToken.Services;

public interface IRefreshTokenService
{
    Task StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiresAt);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeAllUserTokensAsync(string userId);
    Task<string?> GetUserIdFromRefreshTokenAsync(string refreshToken);
}

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
            if (!response.Item.ContainsKey("IsActive") || !response.Item["IsActive"].BOOL)
                return false;

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

    public async Task<string?> GetUserIdFromRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var scanRequest = new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "RefreshToken = :refreshToken AND IsActive = :isActive",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":refreshToken"] = new AttributeValue { S = refreshToken },
                    [":isActive"] = new AttributeValue { BOOL = true }
                }
            };

            var response = await _dynamoClient.ScanAsync(scanRequest);

            if (response.Items.Count == 0)
                return null;

            var item = response.Items.First();

            // Check if token has expired
            if (item.ContainsKey("ExpiresAt"))
            {
                var expiresAt = DateTime.FromFileTimeUtc(long.Parse(item["ExpiresAt"].N));
                if (expiresAt <= DateTime.UtcNow)
                    return null;
            }

            return item["UserId"].S;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user ID from refresh token: {ex.Message}");
            return null;
        }
    }

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
}
