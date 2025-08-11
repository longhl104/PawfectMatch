using Microsoft.AspNetCore.Mvc;
using Amazon.CognitoIdentityProvider.Model;
using Longhl104.Identity.Services;
using Longhl104.Identity.Models;
using Longhl104.PawfectMatch.HttpClient;
using Longhl104.PawfectMatch.Models;
using Amazon.SimpleSystemsManagement.Model;
using Longhl104.PawfectMatch.Utils;
using Longhl104.PawfectMatch.Services;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class RegistrationController(
    ILogger<RegistrationController> _logger,
    IAuthenticationService _authenticationService,
    ICognitoService _cognitoService,
    IInternalHttpClientFactory _internalHttpClientFactory,
    IEmailService _emailService
    ) : ControllerBase
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^\d{11}$")]
    private static partial System.Text.RegularExpressions.Regex AbnRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"^(\+61|0)[2-9][0-9]{8}$")]
    private static partial System.Text.RegularExpressions.Regex AustralianPhoneNumberRegex();

    private readonly HttpClient _matcherHttpClient = _internalHttpClientFactory.CreateClient(PawfectMatchServices.Matcher);
    private readonly HttpClient _shelterHubHttpClient = _internalHttpClientFactory.CreateClient(PawfectMatchServices.ShelterHub);

    /// <summary>
    /// Register a new adopter
    /// </summary>
    [HttpPost("adopter")]
    public async Task<ActionResult<AdopterRegistrationResponse>> RegisterAdopter([FromBody] AdopterRegistrationRequest registrationRequest)
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
            var existingUser = await _cognitoService.CheckIfEmailExistsAsync(registrationRequest.Email);
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
            var userId = await _cognitoService.CreateCognitoAdopterUserAsync(registrationRequest);

            // Save adopter profile to DynamoDB
            await SaveAdopterProfile(registrationRequest, userId);

            _logger.LogInformation("Adopter registration successful for email: {Email}, UserId: {UserId}", registrationRequest.Email, userId);

            // Send welcome email
            try
            {
                var loginUrl = EnvironmentUrlHelper.BuildServiceUrl("matcher", "https://localhost:4201");
                var emailSent = await _emailService.SendWelcomeEmailAsync(
                    registrationRequest.Email,
                    $"{registrationRequest.FirstName} {registrationRequest.LastName}".Trim(),
                    loginUrl
                );

                if (emailSent)
                {
                    _logger.LogInformation("Welcome email sent successfully to {Email}", registrationRequest.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send welcome email to {Email}", registrationRequest.Email);
                }
            }
            catch (Exception emailEx)
            {
                // Don't fail registration if email fails
                _logger.LogError(emailEx, "Error sending welcome email to {Email}", registrationRequest.Email);
            }

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
                    RedirectUrl = EnvironmentUrlHelper.BuildServiceUrl("matcher", "https://localhost:4201"),
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

    private async Task SaveAdopterProfile(AdopterRegistrationRequest request, string userId)
    {
        var response = await _matcherHttpClient.PostAsJsonAsync("adopters", new
        {
            UserId = userId,
            request.FirstName,
            request.LastName
        });

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to save adopter profile for UserId: {UserId}. Error: {Error}", userId, errorMessage);
            throw new InvalidOptionException($"Failed to save adopter profile: {errorMessage}");
        }

        _logger.LogInformation("Adopter profile saved successfully for UserId: {UserId}", userId);
    }

    /// <summary>
    /// Register a new shelter admin
    /// </summary>
    [HttpPost("shelter-admin")]
    public async Task<ActionResult<ShelterAdminRegistrationResponse>> RegisterShelterAdmin([FromBody] ShelterAdminRegistrationRequest registrationRequest)
    {
        try
        {
            _logger.LogInformation("Processing shelter admin registration request for email: {Email}", registrationRequest.Email);

            // Validate required fields
            var validationError = ValidateShelterAdminRegistrationRequest(registrationRequest);
            if (!string.IsNullOrEmpty(validationError))
            {
                return BadRequest(new ShelterAdminRegistrationResponse
                {
                    Success = false,
                    Message = validationError,
                    UserId = string.Empty
                });
            }

            // Check if email already exists
            var existingUser = await _cognitoService.CheckIfEmailExistsAsync(registrationRequest.Email);
            if (existingUser)
            {
                return Conflict(new ShelterAdminRegistrationResponse
                {
                    Success = false,
                    Message = "An account with this email already exists",
                    UserId = string.Empty
                });
            }

            // Create user in Cognito
            var userId = await _cognitoService.CreateCognitoShelterAdminUserAsync(registrationRequest);

            // Save shelter admin profile to DynamoDB
            await SaveShelterAdminProfile(registrationRequest, userId);

            _logger.LogInformation("Shelter admin registration successful for email: {Email}, UserId: {UserId}", registrationRequest.Email, userId);

            // Auto-login the user after successful registration using shared authentication service
            var authResult = await _authenticationService.AuthenticateAndSetCookiesAsync(
                registrationRequest.Email,
                registrationRequest.Password,
                HttpContext,
                _logger,
                tokenData => new ShelterAdminRegistrationResponse
                {
                    Message = "Registration successful",
                    UserId = userId,
                    RedirectUrl = EnvironmentUrlHelper.BuildServiceUrl("shelter", "https://localhost:4202"),
                    Success = true,
                    Data = tokenData
                },
                errorMessage => new ShelterAdminRegistrationResponse
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
            return Conflict(new ShelterAdminRegistrationResponse
            {
                Success = false,
                Message = "An account with this email already exists",
                UserId = string.Empty
            });
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning("Invalid password during registration for email: {Email}. Error: {Error}", registrationRequest.Email, ex.Message);
            return BadRequest(new ShelterAdminRegistrationResponse
            {
                Success = false,
                Message = $"Password does not meet requirements: {ex.Message}",
                UserId = string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during shelter admin registration for email: {Email}", registrationRequest.Email);
            return StatusCode(500, new ShelterAdminRegistrationResponse
            {
                Success = false,
                Message = "An error occurred during registration. Please try again.",
                UserId = string.Empty
            });
        }
    }

    private async Task SaveShelterAdminProfile(ShelterAdminRegistrationRequest request, string userId)
    {
        var response = await _shelterHubHttpClient.PostAsJsonAsync("shelteradmins", new
        {
            UserId = userId,
            request.ShelterName,
            request.ShelterContactNumber,
            request.ShelterAddress,
            request.ShelterWebsiteUrl,
            request.ShelterAbn,
            request.ShelterDescription,
            request.ShelterLatitude,
            request.ShelterLongitude
        });

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to save shelter admin profile for UserId: {UserId}. Error: {Error}", userId, errorMessage);
            throw new InvalidOptionException($"Failed to save shelter admin profile: {errorMessage}");
        }

        _logger.LogInformation("Shelter admin profile saved successfully for UserId: {UserId}", userId);
    }

    private static string ValidateRegistrationRequest(AdopterRegistrationRequest request)
    {
        // Handle both full name and first/last name approaches
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
                return "Full name or both first and last names are required";

            // Populate FullName from FirstName and LastName
            request.FullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}";
        }

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

    private static string ValidateShelterAdminRegistrationRequest(ShelterAdminRegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email is required";

        if (string.IsNullOrWhiteSpace(request.Password))
            return "Password is required";

        if (string.IsNullOrWhiteSpace(request.ShelterName))
            return "Shelter name is required";

        if (string.IsNullOrWhiteSpace(request.ShelterContactNumber))
            return "Shelter contact number is required";

        if (string.IsNullOrWhiteSpace(request.ShelterAddress))
            return "Shelter address is required";

        if (!IsValidEmail(request.Email))
            return "Invalid email format";

        if (request.Password.Length < 8)
            return "Password must be at least 8 characters long";

        // Validate Australian phone number format if provided
        if (!string.IsNullOrWhiteSpace(request.ShelterContactNumber))
        {
            var phoneRegex = AustralianPhoneNumberRegex();
            if (!phoneRegex.IsMatch(request.ShelterContactNumber))
                return "Invalid Australian phone number format";
        }

        // Validate URL format if provided
        if (!string.IsNullOrWhiteSpace(request.ShelterWebsiteUrl))
        {
            if (!Uri.TryCreate(request.ShelterWebsiteUrl, UriKind.Absolute, out _))
                return "Invalid website URL format";
        }

        // Validate ABN format if provided (11 digits)
        if (!string.IsNullOrWhiteSpace(request.ShelterAbn))
        {
            var abnRegex = AbnRegex();
            if (!abnRegex.IsMatch(request.ShelterAbn))
                return "ABN must be 11 digits";
        }

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
}
