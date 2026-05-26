namespace TgTodo.MiniApp.Services;

public class ClientTimezoneDelegatingHandler : DelegatingHandler
{
    private readonly ClientTimezoneState _state;

    public ClientTimezoneDelegatingHandler(ClientTimezoneState state) => _state = state;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_state.Current))
            request.Headers.TryAddWithoutValidation("X-Client-Timezone", _state.Current);
        return base.SendAsync(request, cancellationToken);
    }
}
