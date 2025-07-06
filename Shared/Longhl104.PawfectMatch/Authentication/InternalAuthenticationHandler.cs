using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Longhl104.PawfectMatch.Authentication;

/// <summary>
/// Authentication handler for internal service-to-service communication.
/// Validates API keys or other internal authentication mechanisms.
/// </summary>
public class InternalAuthenticationHandler(
    IOptionsMonitor<InternalAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration
    ) : AuthenticationHandler<InternalAuthenticationOptions>(options, logger, encoder)
{
    private readonly IConfiguration _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for internal API key in headers
        if (!Request.Headers.TryGetValue("X-Internal-API-Key", out var apiKeyValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedApiKey = apiKeyValues.FirstOrDefault();
        if (string.IsNullOrEmpty(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Validate the API key
        var validApiKey = _configuration["InternalApiKey"] ?? throw new InvalidOperationException("Internal API key not configured in app settings");

        if (string.IsNullOrEmpty(validApiKey))
        {
            Logger.LogWarning("No internal API key configured");
            return Task.FromResult(AuthenticateResult.Fail("Internal API key not configured"));
        }

        if (!string.Equals(providedApiKey, validApiKey, StringComparison.Ordinal))
        {
            Logger.LogWarning("Invalid internal API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key"));
        }

        // Create claims for internal service
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "InternalService"),
            new Claim(ClaimTypes.NameIdentifier, "internal-service"),
            new Claim("UserType", "Internal"),
            new Claim("AuthenticationType", "Internal")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("Internal service authenticated successfully");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers.Append("WWW-Authenticate", $"{Scheme.Name} realm=\"Internal API\"");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Options for internal authentication
/// </summary>
public class InternalAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "Internal";

    /// <summary>
    /// The header name to look for the API key. Default is "X-Internal-API-Key"
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-Internal-API-Key";

    /// <summary>
    /// Whether to allow internal authentication to bypass other authorization requirements
    /// </summary>
    public bool BypassUserAuthorization { get; set; } = true;
}
