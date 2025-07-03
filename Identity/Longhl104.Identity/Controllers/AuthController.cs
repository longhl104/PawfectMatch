using Longhl104.Identity.Models;
using Longhl104.Identity.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ICognitoService cognitoService,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
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

            // Authenticate user with Cognito
            var (success, message, user) = await cognitoService.AuthenticateUserAsync(
                loginRequest.Email,
                loginRequest.Password);

            if (!success || user == null)
            {
                logger.LogWarning("Authentication failed for user {Email}: {Message}", loginRequest.Email, message);
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = message,
                    Data = null
                });
            }

            // Generate tokens
            var accessToken = jwtService.GenerateAccessToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 1 hour for access token

            // Store refresh token
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days for refresh token
            await refreshTokenService.StoreRefreshTokenAsync(user.UserId, refreshToken, refreshTokenExpiresAt);

            // Create response
            var tokenData = new TokenData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = user
            };

            var loginResponse = new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Data = tokenData
            };

            logger.LogInformation("User {Email} logged in successfully", loginRequest.Email);

            // Set cookies
            SetAuthenticationCookies(accessToken, refreshToken, user);

            return Ok(loginResponse);
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

            // Generate new tokens
            var newAccessToken = jwtService.GenerateAccessToken(userProfile);
            var newRefreshToken = jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // 1 hour for access token

            // Revoke old refresh token and store new one
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days for new refresh token
            await refreshTokenService.RevokeRefreshTokenAsync(userId, refreshTokenRequest.RefreshToken);
            await refreshTokenService.StoreRefreshTokenAsync(userId, newRefreshToken, refreshTokenExpiresAt);

            // Create response
            var tokenData = new TokenData
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                User = userProfile
            };

            var refreshResponse = new RefreshTokenResponse
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = tokenData
            };

            logger.LogInformation("Tokens refreshed successfully for user {UserId}", userId);

            // Set new cookies
            SetAuthenticationCookies(newAccessToken, newRefreshToken, userProfile);

            return Ok(refreshResponse);
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
            ClearAuthenticationCookies();

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

    private void SetAuthenticationCookies(string accessToken, string refreshToken, UserProfile user)
    {
        var cookieDomain = HttpContext.Request.Host.Host;
        var isSecure = HttpContext.Request.IsHttps;
        var sameSite = isSecure ? SameSiteMode.None : SameSiteMode.Lax;

        // Access token cookie
        Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromHours(1),
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSite
        });

        // Refresh token cookie
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromDays(30),
            HttpOnly = true,
            Secure = isSecure,
            SameSite = sameSite
        });

        // User info cookie (non-HttpOnly for client access)
        var userInfoJson = JsonSerializer.Serialize(user, CamelCaseOptions);
        var userInfoBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userInfoJson));
        Response.Cookies.Append("userInfo", userInfoBase64, new CookieOptions
        {
            Path = "/",
            Domain = cookieDomain.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? null : cookieDomain,
            MaxAge = TimeSpan.FromHours(1),
            HttpOnly = false,
            Secure = isSecure,
            SameSite = sameSite
        });
    }

    private void ClearAuthenticationCookies()
    {
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        Response.Cookies.Delete("userInfo");
    }
}
