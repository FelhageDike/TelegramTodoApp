using Microsoft.Extensions.Options;

namespace TgTodo.AspNetCore.Auth;

public sealed class ServiceAuthDelegatingHandler : DelegatingHandler
{
    private readonly ServiceAuthOptions _authOptions;

    public ServiceAuthDelegatingHandler(IOptionsMonitor<ServiceAuthOptions> authOptions) =>
        _authOptions = authOptions.CurrentValue;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_authOptions.InternalKey))
        {
            request.Headers.Remove(TgTodoAuthDefaults.ServiceKeyHeaderName);
            request.Headers.TryAddWithoutValidation(TgTodoAuthDefaults.ServiceKeyHeaderName, _authOptions.InternalKey);
        }

        if (!string.IsNullOrEmpty(_authOptions.UserIdSigningKey) &&
            request.Headers.TryGetValues(TgTodoAuthDefaults.UserIdHeaderName, out var userIds))
        {
            var userId = userIds.First();
            request.Headers.Remove(TgTodoAuthDefaults.UserIdSignatureHeaderName);
            request.Headers.TryAddWithoutValidation(
                TgTodoAuthDefaults.UserIdSignatureHeaderName,
                UserIdSignatureHelper.Sign(userId, _authOptions.UserIdSigningKey));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
