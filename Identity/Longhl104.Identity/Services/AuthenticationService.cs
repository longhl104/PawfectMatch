using Longhl104.Identity.Models;
using Microsoft.AspNetCore.Mvc;

namespace Longhl104.Identity.Services;

/// <summary>
/// Interface for common authentication operations
/// </summary>
public interface IAuthenticationService
{
    Task<IActionResult> AuthenticateAndSetCookiesAsync<T>(
        string email,
        string password,
        HttpContext httpContext,
        ILogger logger,
        Func<TokenData, T> createSuccessResponse,
        Func<string, T> createFailureResponse) where T : class;
}

/// <summary>
/// Service for handling common authentication workflows
/// </summary>
public class AuthenticationService(ICognitoService cognitoService, ICookieService cookieService) : IAuthenticationService
{

    /// <summary>
    /// Authenticates user with Cognito OIDC and sets cookies
    /// </summary>
    /// <typeparam name="T">Response type</typeparam>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="httpContext">HTTP context for setting cookies</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="createSuccessResponse">Function to create success response with token data</param>
    /// <param name="createFailureResponse">Function to create failure response with error message</param>
    /// <returns>Action result with authentication response</returns>
    public async Task<IActionResult> AuthenticateAndSetCookiesAsync<T>(
        string email,
        string password,
        HttpContext httpContext,
        ILogger logger,
        Func<TokenData, T> createSuccessResponse,
        Func<string, T> createFailureResponse) where T : class
    {
        try
        {
            // Authenticate user with Cognito using OIDC tokens
            var (authSuccess, authMessage, cognitoTokens, userProfile) = await cognitoService.AuthenticateWithTokensAsync(email, password);

            if (!authSuccess || cognitoTokens == null || userProfile == null)
            {
                logger.LogWarning("Authentication failed for user {Email}: {Message}", email, authMessage);
                var failureResponse = createFailureResponse(authMessage);
                return new UnauthorizedObjectResult(failureResponse);
            }

            // Set JWT cookies
            cookieService.SetJwtAuthenticationCookies(httpContext, cognitoTokens.AccessToken, userProfile);

            // Create token data for response with JWT tokens
            var tokenData = new TokenData
            {
                AccessToken = cognitoTokens.AccessToken,
                ExpiresAt = cognitoTokens.ExpiresAt,
                User = userProfile
            };

            logger.LogInformation("User {Email} authenticated successfully with JWT tokens", email);

            var successResponse = createSuccessResponse(tokenData);
            return new OkObjectResult(successResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authentication for user: {Email}", email);
            var errorResponse = createFailureResponse("Authentication failed due to server error");
            return new ObjectResult(errorResponse) { StatusCode = 500 };
        }
    }
}
