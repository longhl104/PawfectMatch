using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using RefreshToken.Models;

namespace RefreshToken.Services;

public interface ICognitoService
{
    Task<UserProfile?> GetUserProfileAsync(string email);
}

public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _clientId;

    public CognitoService()
    {
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
        _userPoolId = Environment.GetEnvironmentVariable("USER_POOL_ID") ?? throw new InvalidOperationException("USER_POOL_ID environment variable is required");
        _clientId = Environment.GetEnvironmentVariable("USER_POOL_CLIENT_ID") ?? throw new InvalidOperationException("USER_POOL_CLIENT_ID environment variable is required");
    }

    public async Task<UserProfile?> GetUserProfileAsync(string email)
    {
        try
        {
            var getUserRequest = new AdminGetUserRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            };

            var getUserResponse = await _cognitoClient.AdminGetUserAsync(getUserRequest);

            var userProfile = new UserProfile
            {
                UserId = getUserResponse.Username,
                Email = email,
                CreatedAt = getUserResponse.UserCreateDate,
                LastLoginAt = DateTime.UtcNow
            };

            // Extract user attributes
            foreach (var attribute in getUserResponse.UserAttributes)
            {
                switch (attribute.Name)
                {
                    case "email":
                        userProfile.Email = attribute.Value;
                        break;
                    case "phone_number":
                        userProfile.PhoneNumber = attribute.Value;
                        break;
                    case "custom:user_type":
                        userProfile.UserType = attribute.Value;
                        break;
                }
            }

            return userProfile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user profile: {ex.Message}");
            return null;
        }
    }
}
