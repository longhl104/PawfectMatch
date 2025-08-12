using Longhl104.Identity.Models;
using Longhl104.Identity.Services;
using Microsoft.AspNetCore.Mvc;

namespace Longhl104.Identity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthenticationService authenticationService,
    ICookieService cookieService,
    ICognitoService cognitoService,
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

    /// <summary>
    /// Initiate password reset process
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        logger.LogInformation("Password reset requested for email: {Email}", request.Email);

        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Valid email is required"
                });
            }

            // Initiate password reset with Cognito
            var (Success, Message) = await cognitoService.InitiatePasswordResetAsync(request.Email);

            if (Success)
            {
                return Ok(new ForgotPasswordResponse
                {
                    Success = true,
                    Message = Message
                });
            }

            return BadRequest(new ForgotPasswordResponse
            {
                Success = false,
                Message = Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during password reset initiation for email: {Email}", request.Email);
            return StatusCode(500, new ForgotPasswordResponse
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Confirm password reset with verification code
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        logger.LogInformation("Password reset confirmation for email: {Email}", request.Email);

        try
        {
            // Validate input
            var (IsValid, ErrorMessage) = ValidateResetPasswordRequest(request);
            if (!IsValid)
            {
                return BadRequest(new ResetPasswordResponse
                {
                    Success = false,
                    Message = ErrorMessage
                });
            }

            // Confirm password reset with Cognito
            var (Success, Message) = await cognitoService.ConfirmPasswordResetAsync(
                request.Email,
                request.ResetCode,
                request.NewPassword
            );

            if (Success)
            {
                return Ok(new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Password has been reset successfully"
                });
            }

            return BadRequest(new ResetPasswordResponse
            {
                Success = false,
                Message = Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during password reset confirmation for email: {Email}", request.Email);
            return StatusCode(500, new ResetPasswordResponse
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }

    private static (bool IsValid, string ErrorMessage) ValidateResetPasswordRequest(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return (false, "Email is required");
        }

        if (!IsValidEmail(request.Email))
        {
            return (false, "Invalid email format");
        }

        if (string.IsNullOrWhiteSpace(request.ResetCode))
        {
            return (false, "Reset code is required");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return (false, "New password is required");
        }

        if (request.NewPassword.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        // Add more password complexity validation if needed
        // For example: uppercase, lowercase, numbers, special characters

        return (true, string.Empty);
    }
}
