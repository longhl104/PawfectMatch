using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Longhl104.PawfectMatch.Authentication;

/// <summary>
/// Custom authentication handler that works with the PawfectMatch AuthenticationMiddleware.
/// The middleware already sets the user principal, so this handler just needs to validate it.
/// </summary>
public class PawfectMatchAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
    ) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // The AuthenticationMiddleware has already set the user principal
        // Check if we have a user with the correct authentication type
        if (Context.User?.Identity?.IsAuthenticated == true &&
            Context.User.Identity.AuthenticationType == "PawfectMatch"
            )
        {
            var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // The AuthenticationMiddleware handles redirects, so we don't need to do anything here
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}
