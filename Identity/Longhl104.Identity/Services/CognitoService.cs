using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Longhl104.PawfectMatch.Models.Identity;
using Longhl104.Identity.Models;

namespace Longhl104.Identity.Services;

/// <summary>
/// Interface for Cognito user authentication and management
/// </summary>
public interface ICognitoService
{
    Task<(bool Success, string Message, UserProfile? User)> AuthenticateUserAsync(string email, string password);
    Task<UserProfile?> GetUserProfileAsync(string email);
    Task<(bool Success, string Message, CognitoTokens? Tokens, UserProfile? User)> AuthenticateWithTokensAsync(string email, string password);
    Task<bool> CheckIfEmailExistsAsync(string email);
    Task<string> CreateCognitoAdopterUserAsync(AdopterRegistrationRequest request);
    Task<string> CreateCognitoShelterAdminUserAsync(ShelterAdminRegistrationRequest request);
}

/// <summary>
/// Model for Cognito OIDC tokens
/// </summary>
public class CognitoTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Service for managing AWS Cognito user operations
/// </summary>
public class CognitoService : ICognitoService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly string _userPoolId;
    private readonly string _clientId;

    public CognitoService(IConfiguration configuration)
    {
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
        _userPoolId = configuration["UserPoolId"] ?? throw new InvalidOperationException("UserPoolId configuration is required");
        _clientId = configuration["UserPoolClientId"] ?? throw new InvalidOperationException("UserPoolClientId configuration is required");
    }

    public CognitoService(IAmazonCognitoIdentityProvider cognitoClient, string userPoolId, string clientId)
    {
        _cognitoClient = cognitoClient;
        _userPoolId = userPoolId;
        _clientId = clientId;
    }

    /// <summary>
    /// Authenticates a user with email and password and returns OIDC tokens
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <returns>Authentication result with success status, message, tokens, and user profile</returns>
    public async Task<(bool Success, string Message, CognitoTokens? Tokens, UserProfile? User)> AuthenticateWithTokensAsync(string email, string password)
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

                    // Create tokens object
                    var tokens = new CognitoTokens
                    {
                        AccessToken = authResponse.AuthenticationResult.AccessToken,
                        IdToken = authResponse.AuthenticationResult.IdToken,
                        RefreshToken = authResponse.AuthenticationResult.RefreshToken,
                        ExpiresIn = authResponse.AuthenticationResult.ExpiresIn ?? 3600, // Default to 1 hour
                        ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.AuthenticationResult.ExpiresIn ?? 3600)
                    };

                    return (true, "Authentication successful", tokens, userProfile);
                }

                return (false, "User profile not found", null, null);
            }

            return (false, "Authentication failed", null, null);
        }
        catch (NotAuthorizedException)
        {
            return (false, "Invalid email or password", null, null);
        }
        catch (UserNotConfirmedException)
        {
            return (false, "User account is not confirmed", null, null);
        }
        catch (PasswordResetRequiredException)
        {
            return (false, "Password reset is required", null, null);
        }
        catch (UserNotFoundException)
        {
            return (false, "User not found", null, null);
        }
        catch (TooManyRequestsException)
        {
            return (false, "Too many requests. Please try again later", null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication error: {ex.Message}");
            return (false, "Authentication failed due to server error", null, null);
        }
    }

    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <returns>Authentication result with success status, message, and user profile</returns>
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

    /// <summary>
    /// Retrieves user profile information from Cognito
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>User profile or null if not found</returns>
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

            if (!getUserResponse.UserCreateDate.HasValue)
            {
                throw new InvalidOperationException("User creation date is not available.");
            }

            var userProfile = new UserProfile
            {
                UserId = getUserResponse.Username,
                Email = email,
                CreatedAt = getUserResponse.UserCreateDate.Value,
                LastLoginAt = DateTime.UtcNow,
                FullName = getUserResponse.UserAttributes.FirstOrDefault(attr => attr.Name == "given_name")?.Value ?? string.Empty
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

    /// <summary>
    /// Checks if an email already exists in Cognito
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if email exists, false otherwise</returns>
    public async Task<bool> CheckIfEmailExistsAsync(string email)
    {
        try
        {
            var request = new AdminGetUserRequest
            {
                UserPoolId = _userPoolId,
                Username = email
            };

            await _cognitoClient.AdminGetUserAsync(request);
            return true; // User exists
        }
        catch (UserNotFoundException)
        {
            return false; // User doesn't exist
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if email exists: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates a new adopter user in Cognito
    /// </summary>
    /// <param name="request">Adopter registration details</param>
    /// <returns>User ID of the created user</returns>
    public async Task<string> CreateCognitoAdopterUserAsync(AdopterRegistrationRequest request)
    {
        var userAttributes = new List<AttributeType>
        {
            new() { Name = "email", Value = request.Email },
            new() { Name = "custom:user_type", Value = "adopter" },
            new() { Name = "email_verified", Value = "true" } // Assuming email is verified at registration
        };

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            userAttributes.Add(new AttributeType { Name = "phone_number_verified", Value = "true" });
            var australianPhoneNumber = request.PhoneNumber.StartsWith("+61") ? request.PhoneNumber : $"+61{request.PhoneNumber.TrimStart('0')}";
            userAttributes.Add(new AttributeType { Name = "phone_number", Value = australianPhoneNumber });
        }

        var adminCreateUserRequest = new AdminCreateUserRequest
        {
            UserPoolId = _userPoolId,
            Username = request.Email,
            UserAttributes = userAttributes,
            TemporaryPassword = request.Password,
            MessageAction = MessageActionType.SUPPRESS, // Don't send welcome email
            DesiredDeliveryMediums = ["EMAIL"]
        };

        var response = await _cognitoClient.AdminCreateUserAsync(adminCreateUserRequest);

        // Set permanent password
        var setPasswordRequest = new AdminSetUserPasswordRequest
        {
            UserPoolId = _userPoolId,
            Username = request.Email,
            Password = request.Password,
            Permanent = true
        };

        await _cognitoClient.AdminSetUserPasswordAsync(setPasswordRequest);

        Console.WriteLine($"Created Cognito user for email: {request.Email}");

        return response.User.Username;
    }

    /// <summary>
    /// Creates a new shelter admin user in Cognito
    /// </summary>
    /// <param name="request">Shelter admin registration details</param>
    /// <returns>User ID of the created user</returns>
    public async Task<string> CreateCognitoShelterAdminUserAsync(ShelterAdminRegistrationRequest request)
    {
        var userAttributes = new List<AttributeType>
        {
            new() { Name = "email", Value = request.Email },
            new() { Name = "custom:user_type", Value = "shelter_admin" },
            new() { Name = "email_verified", Value = "true" } // Assuming email is verified at registration
        };

        var adminCreateUserRequest = new AdminCreateUserRequest
        {
            UserPoolId = _userPoolId,
            Username = request.Email,
            UserAttributes = userAttributes,
            TemporaryPassword = request.Password,
            MessageAction = MessageActionType.SUPPRESS, // Don't send welcome email
            DesiredDeliveryMediums = ["EMAIL"]
        };

        var response = await _cognitoClient.AdminCreateUserAsync(adminCreateUserRequest);

        // Set permanent password
        var setPasswordRequest = new AdminSetUserPasswordRequest
        {
            UserPoolId = _userPoolId,
            Username = request.Email,
            Password = request.Password,
            Permanent = true
        };

        await _cognitoClient.AdminSetUserPasswordAsync(setPasswordRequest);

        Console.WriteLine($"Created Cognito shelter admin user for email: {request.Email}");

        return response.User.Username;
    }
}
