namespace TgTodo.MiniApp.Services;

public sealed class ToastEntry
{
    public Guid Id { get; init; }
    public string Message { get; init; } = "";
    public ToastType Type { get; init; }
}

public sealed class ToastService : IDisposable
{
    private const int VisibleMs = 2600;
    private const int MaxVisible = 2;

    private readonly List<ToastEntry> _items = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = new();

    public event Action? Changed;

    public IReadOnlyList<ToastEntry> Items => _items;

    public void Show(string message, ToastType type = ToastType.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var text = message.Trim();
        if (_items.Any(t => t.Message == text && t.Type == type))
            return;

        while (_items.Count >= MaxVisible)
            Dismiss(_items[0].Id);

        var entry = new ToastEntry
        {
            Id = Guid.NewGuid(),
            Message = text,
            Type = type
        };

        _items.Add(entry);
        Changed?.Invoke();

        var cts = new CancellationTokenSource();
        _timers[entry.Id] = cts;
        _ = AutoDismissAsync(entry.Id, cts.Token);
    }

    public void Dismiss(Guid id)
    {
        var removed = _items.RemoveAll(t => t.Id == id);
        if (removed == 0)
            return;

        if (_timers.Remove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        Changed?.Invoke();
    }

    private async Task AutoDismissAsync(Guid id, CancellationToken ct)
    {
        try
        {
            await Task.Delay(VisibleMs, ct);
            if (!ct.IsCancellationRequested)
                Dismiss(id);
        }
        catch (TaskCanceledException)
        {
            // replaced or disposed
        }
    }

    public void Dispose()
    {
        foreach (var cts in _timers.Values)
            cts.Dispose();
        _timers.Clear();
        _items.Clear();
    }
}
