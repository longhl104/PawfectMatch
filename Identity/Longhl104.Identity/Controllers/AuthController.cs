using Longhl104.Identity.Models;
using Longhl104.Identity.Services;
using Longhl104.PawfectMatch.Models.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthenticationService authenticationService,
    ICognitoService cognitoService,
    IRefreshTokenService refreshTokenService,
    ICookieService cookieService,
    ILogger<AuthController> logger
    ) : ControllerBase
{

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Authenticate user and return tokens
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        logger.LogInformation("Login request for user: {Email}", loginRequest.Email);

        try
        {
            // Validate input
            var (IsValid, ErrorMessage) = ValidateLoginRequest(loginRequest);
            if (!IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ErrorMessage,
                    Data = null
                });
            }

            // Use shared authentication service
            return await authenticationService.AuthenticateAndSetCookiesAsync<LoginResponse>(
                loginRequest.Email,
                loginRequest.Password,
                HttpContext,
                logger,
                tokenData => new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Data = tokenData
                },
                errorMessage => new LoginResponse
                {
                    Success = false,
                    Message = errorMessage,
                    Data = null
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for user: {Email}", loginRequest.Email);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
    {
        logger.LogInformation("Refresh token request");

        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(refreshTokenRequest.RefreshToken))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Refresh token is required",
                    Data = null
                });
            }

            // Get user ID from refresh token
            var userId = await refreshTokenService.GetUserIdFromRefreshTokenAsync(refreshTokenRequest.RefreshToken);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Invalid or expired refresh token");
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid or expired refresh token",
                    Data = null
                });
            }

            // Validate refresh token
            var isValidToken = await refreshTokenService.ValidateRefreshTokenAsync(userId, refreshTokenRequest.RefreshToken);
            if (!isValidToken)
            {
                logger.LogWarning("Invalid refresh token for user {UserId}", userId);
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid or expired refresh token",
                    Data = null
                });
            }

            // Get user profile
            var userProfile = await cognitoService.GetUserProfileAsync(userId);
            if (userProfile == null)
            {
                logger.LogWarning("User profile not found for user {UserId}", userId);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found",
                    Data = null
                });
            }

            // Note: For OIDC workflow, we would need to implement Cognito's refresh token flow
            // For now, returning an error to prompt re-authentication
            logger.LogWarning("Refresh token functionality not implemented for OIDC workflow. User should re-authenticate.");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Token refresh not available. Please log in again.",
                Data = null
            });

            // TODO: Implement Cognito refresh token flow using:
            // - AdminInitiateAuthRequest with AuthFlow = REFRESH_TOKEN_AUTH
            // - Pass the refresh token in AuthParameters["REFRESH_TOKEN"]
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }

    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        try
        {
            // Clear cookies
            cookieService.ClearAuthenticationCookies(HttpContext);

            // TODO: Implement token blacklisting if needed

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Logged out successfully",
                Data = null
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during logout");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error",
                Data = null
            });
        }
    }

    private static (bool IsValid, string ErrorMessage) ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (false, "Email is required");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "Password is required");
        }

        if (!IsValidEmail(request.Email))
        {
            return (false, "Invalid email format");
        }

        if (request.Password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        return (true, string.Empty);
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
