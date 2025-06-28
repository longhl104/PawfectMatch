using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Login.Models;

namespace Login.Services;

public interface ICognitoService
{
    Task<(bool Success, string Message, UserProfile? User)> AuthenticateUserAsync(string email, string password);
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

    public async Task<(bool Success, string Message, UserProfile? User)> AuthenticateUserAsync(string email, string password)
    {
        try
        {
            var authRequest = new AdminInitiateAuthRequest
            {
                UserPoolId = _userPoolId,
                ClientId = _clientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>
                {
                    ["USERNAME"] = email,
                    ["PASSWORD"] = password
                }
            };

            var authResponse = await _cognitoClient.AdminInitiateAuthAsync(authRequest);

            if (authResponse.AuthenticationResult != null)
            {
                // Get user details
                var userProfile = await GetUserProfileAsync(email);
                if (userProfile != null)
                {
                    userProfile.LastLoginAt = DateTime.UtcNow;
                    return (true, "Authentication successful", userProfile);
                }

                return (false, "User profile not found", null);
            }

            return (false, "Authentication failed", null);
        }
        catch (NotAuthorizedException)
        {
            return (false, "Invalid email or password", null);
        }
        catch (UserNotConfirmedException)
        {
            return (false, "User account is not confirmed", null);
        }
        catch (PasswordResetRequiredException)
        {
            return (false, "Password reset is required", null);
        }
        catch (UserNotFoundException)
        {
            return (false, "User not found", null);
        }
        catch (TooManyRequestsException)
        {
            return (false, "Too many requests. Please try again later", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication error: {ex.Message}");
            return (false, "Authentication failed due to server error", null);
        }
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
