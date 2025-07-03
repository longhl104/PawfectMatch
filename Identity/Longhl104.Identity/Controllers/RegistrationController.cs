using Microsoft.AspNetCore.Mvc;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Longhl104.Identity.Services;
using Longhl104.Identity.Models;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
    private readonly ILogger<RegistrationController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ICookieService _cookieService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly string _tableName;
    private readonly string _userPoolId;

    public RegistrationController(
        AmazonDynamoDBClient dynamoDbClient,
        AmazonCognitoIdentityProviderClient cognitoClient,
        ILogger<RegistrationController> logger,
        IConfiguration configuration,
        ICookieService cookieService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IHostEnvironment hostEnvironment
        )
    {
        _dynamoDbClient = dynamoDbClient;
        _cognitoClient = cognitoClient;
        _logger = logger;
        _configuration = configuration;
        _cookieService = cookieService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _tableName = $"pawfect-match-adopters-{hostEnvironment.EnvironmentName.ToLowerInvariant()}";
        _userPoolId = _configuration["AWS:UserPoolId"] ?? throw new InvalidOperationException("AWS:UserPoolId configuration is required");
    }

    /// <summary>
    /// Register a new adopter
    /// </summary>
    [HttpPost("adopter")]
    public async Task<IActionResult> RegisterAdopter([FromBody] AdopterRegistrationRequest registrationRequest)
    {
        try
        {
            _logger.LogInformation("Processing adopter registration request for email: {Email}", registrationRequest.Email);

            // Validate required fields
            var validationError = ValidateRegistrationRequest(registrationRequest);
            if (!string.IsNullOrEmpty(validationError))
            {
                return BadRequest(new AdopterRegistrationResponse
                {
                    Success = false,
                    Message = validationError,
                    UserId = string.Empty
                });
            }

            // Check if email already exists
            var existingUser = await CheckIfEmailExists(registrationRequest.Email);
            if (existingUser)
            {
                return Conflict(new AdopterRegistrationResponse
                {
                    Success = false,
                    Message = "An account with this email already exists",
                    UserId = string.Empty
                });
            }

            // Create user in Cognito
            var userId = await CreateCognitoUser(registrationRequest);

            // Save adopter profile to DynamoDB
            await SaveAdopterProfile(registrationRequest, userId);

            _logger.LogInformation("Adopter registration successful for email: {Email}, UserId: {UserId}", registrationRequest.Email, userId);

            // Auto-login the user after successful registration
            var userProfile = new UserProfile
            {
                UserId = userId,
                Email = registrationRequest.Email,
                UserType = "adopter",
                PhoneNumber = registrationRequest.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(userProfile);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 1 hour for access token

            // Store refresh token
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days for refresh token
            await _refreshTokenService.StoreRefreshTokenAsync(userId, refreshToken, refreshTokenExpiresAt);

            // Set authentication cookies
            _cookieService.SetJwtAuthenticationCookies(HttpContext, accessToken, refreshToken, userProfile);

            // Create token data for response
            var tokenData = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = userProfile
            };

            // Return success response with tokens and redirect URL for Angular to handle
            return Ok(new AdopterRegistrationResponse
            {
                Message = "Registration successful",
                UserId = userId,
                RedirectUrl = "https://localhost:4201",
                Success = true,
                Data = tokenData
            });
        }
        catch (UsernameExistsException)
        {
            _logger.LogWarning("Registration attempted for existing email: {Email}", registrationRequest.Email);
            return Conflict(new AdopterRegistrationResponse
            {
                Success = false,
                Message = "An account with this email already exists",
                UserId = string.Empty
            });
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning("Invalid password during registration for email: {Email}. Error: {Error}", registrationRequest.Email, ex.Message);
            return BadRequest(new AdopterRegistrationResponse
            {
                Success = false,
                Message = $"Password does not meet requirements: {ex.Message}",
                UserId = string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during adopter registration for email: {Email}", registrationRequest.Email);
            return StatusCode(500, new AdopterRegistrationResponse
            {
                Success = false,
                Message = "An error occurred during registration. Please try again.",
                UserId = string.Empty
            });
        }
    }

    private static string ValidateRegistrationRequest(AdopterRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return "Full name is required";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required";

        if (string.IsNullOrWhiteSpace(request.Address))
            return "Address is required";

        if (!IsValidEmail(request.Email))
            return "Invalid email format";

        if (request.Password.Length < 8)
            return "Password must be at least 8 characters long";

        return string.Empty;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckIfEmailExists(string email)
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
            _logger.LogError(ex, "Error checking if email exists: {Email}", email);
            throw;
        }
    }

    private async Task<string> CreateCognitoUser(AdopterRegistrationRequest request)
    {
        var userAttributes = new List<AttributeType>
        {
            new() { Name = "email", Value = request.Email },
            new() { Name = "given_name", Value = request.FullName },
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

        _logger.LogInformation("Created Cognito user for email: {Email}", request.Email);

        return response.User.Username;
    }

    private async Task SaveAdopterProfile(AdopterRegistrationRequest request, string userId)
    {
        var adopterItem = new Dictionary<string, AttributeValue>
        {
            ["UserId"] = new() { S = userId },
            ["FullName"] = new() { S = request.FullName },
            ["Email"] = new() { S = request.Email },
            ["Address"] = new() { S = request.Address },
            ["CreatedAt"] = new() { S = DateTime.UtcNow.ToString("O") },
            ["UserType"] = new() { S = "adopter" }
        };

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            adopterItem["PhoneNumber"] = new AttributeValue { S = request.PhoneNumber };
        }

        if (!string.IsNullOrWhiteSpace(request.Bio))
        {
            adopterItem["Bio"] = new AttributeValue { S = request.Bio };
        }

        var putItemRequest = new PutItemRequest
        {
            TableName = _tableName,
            Item = adopterItem
        };

        await _dynamoDbClient.PutItemAsync(putItemRequest);

        _logger.LogInformation("Saved adopter profile to DynamoDB for UserId: {UserId}", userId);
    }
}

public class AdopterRegistrationRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? Bio { get; set; }
}

public class AdopterRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public TokenData? Data { get; set; }
}
