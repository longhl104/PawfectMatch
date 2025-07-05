using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Longhl104.Identity.Services;
using Longhl104.Identity.Models;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
    private readonly ILogger<RegistrationController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationService _authenticationService; // Use shared authentication service
    private readonly string _userPoolId;

    public RegistrationController(
        AmazonCognitoIdentityProviderClient cognitoClient,
        ILogger<RegistrationController> logger,
        IConfiguration configuration,
        IAuthenticationService authenticationService, // Inject shared authentication service
        IHostEnvironment hostEnvironment
        )
    {
        _cognitoClient = cognitoClient;
        _logger = logger;
        _configuration = configuration;
        _authenticationService = authenticationService;
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
            // await SaveAdopterProfile(registrationRequest, userId);

            _logger.LogInformation("Adopter registration successful for email: {Email}, UserId: {UserId}", registrationRequest.Email, userId);

            // Auto-login the user after successful registration using shared authentication service
            var authResult = await _authenticationService.AuthenticateAndSetCookiesAsync(
                registrationRequest.Email,
                registrationRequest.Password,
                HttpContext,
                _logger,
                tokenData => new AdopterRegistrationResponse
                {
                    Message = "Registration successful",
                    UserId = userId,
                    RedirectUrl = "https://localhost:4201",
                    Success = true,
                    Data = tokenData
                },
                errorMessage => new AdopterRegistrationResponse
                {
                    Message = "Registration successful, but auto-login failed. Please login manually.",
                    UserId = userId,
                    Success = true
                }
            );

            return authResult;
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

public class ShelterAdminRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ShelterName { get; set; } = string.Empty;
    public string ShelterContactNumber { get; set; } = string.Empty;
    public string ShelterAddress { get; set; } = string.Empty;
    public string? ShelterWebsiteUrl { get; set; }
    public string? ShelterAbn { get; set; }
    public string? ShelterDescription { get; set; }
}

public class ShelterAdminRegistrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public TokenData? Data { get; set; }
}
