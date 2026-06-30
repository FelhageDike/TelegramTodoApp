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
            return Task.FromResult(AuthenticateResult.Fail("Missing or invalid X-User-Id header."));
        }

        if (_authOptions.RequireServiceKeyForUserApi &&
            !string.IsNullOrEmpty(_authOptions.InternalKey))
        {
            if (!Request.Headers.TryGetValue(TgTodoAuthDefaults.ServiceKeyHeaderName, out var serviceKey) ||
                serviceKey != _authOptions.InternalKey)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid or missing X-TgTodo-Service-Key header."));
            }
        }

        var userIdValue = userId.ToString();
        var signingKey = _authOptions.UserIdSigningKey;

        if (!string.IsNullOrEmpty(signingKey))
        {
            if (!Request.Headers.TryGetValue(TgTodoAuthDefaults.UserIdSignatureHeaderName, out var signature) ||
                !UserIdSignatureHelper.Verify(userIdValue, signature.ToString(), signingKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid or missing X-User-Id-Signature header."));
            }
        }

        var claims = new[] { new Claim(TgTodoAuthDefaults.UserIdClaimType, userIdValue) };
        var identity = new ClaimsIdentity(claims, TgTodoAuthDefaults.UserSchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TgTodoAuthDefaults.UserSchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
