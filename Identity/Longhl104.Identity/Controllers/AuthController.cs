using Longhl104.Identity.Models;
using Longhl104.Identity.Services;
using Microsoft.AspNetCore.Mvc;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthenticationService authenticationService,
    ICookieService cookieService,
    ILogger<AuthController> logger
    ) : ControllerBase
{
    /// <summary>
    /// Authenticate user and return tokens
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest loginRequest)
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
            return await authenticationService.AuthenticateAndSetCookiesAsync(
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
