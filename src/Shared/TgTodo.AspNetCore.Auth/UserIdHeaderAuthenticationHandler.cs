using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TgTodo.AspNetCore.Auth;

public sealed class UserIdHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public UserIdHeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TgTodoAuthDefaults.UserIdHeaderName, out var header) ||
            !Guid.TryParse(header, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[] { new Claim(TgTodoAuthDefaults.UserIdClaimType, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, TgTodoAuthDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TgTodoAuthDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
