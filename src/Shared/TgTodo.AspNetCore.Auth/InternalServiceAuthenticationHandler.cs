using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TgTodo.AspNetCore.Auth;

public sealed class InternalServiceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ServiceAuthOptions _authOptions;

    public InternalServiceAuthenticationHandler(
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
        var expectedKey = _authOptions.InternalKey;
        if (string.IsNullOrEmpty(expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("Internal service key is not configured."));

        if (!Request.Headers.TryGetValue(TgTodoAuthDefaults.ServiceKeyHeaderName, out var header) ||
            header != expectedKey)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[] { new Claim("tgtodo:service", "internal") };
        var identity = new ClaimsIdentity(claims, TgTodoAuthDefaults.InternalSchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TgTodoAuthDefaults.InternalSchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
