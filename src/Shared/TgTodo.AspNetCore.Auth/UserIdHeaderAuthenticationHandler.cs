using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TgTodo.AspNetCore.Auth;

public sealed class UserIdHeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ServiceAuthOptions _authOptions;

    public UserIdHeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<ServiceAuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        _authOptions = authOptions.CurrentValue;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TgTodoAuthDefaults.UserIdHeaderName, out var header) ||
            !Guid.TryParse(header, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userIdValue = userId.ToString();
        var signingKey = _authOptions.UserIdSigningKey;

        if (!string.IsNullOrEmpty(signingKey))
        {
            if (!Request.Headers.TryGetValue(TgTodoAuthDefaults.UserIdSignatureHeaderName, out var signature) ||
                !UserIdSignatureHelper.Verify(userIdValue, signature.ToString(), signingKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }

        var claims = new[] { new Claim(TgTodoAuthDefaults.UserIdClaimType, userIdValue) };
        var identity = new ClaimsIdentity(claims, TgTodoAuthDefaults.UserSchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TgTodoAuthDefaults.UserSchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
