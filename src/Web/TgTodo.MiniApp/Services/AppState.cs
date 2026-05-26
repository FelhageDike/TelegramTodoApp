namespace TgTodo.MiniApp.Services;

public class AppState
{
    private readonly GroupSelectionStorage _storage;

    public AppState(GroupSelectionStorage storage) => _storage = storage;

    public Guid? SelectedGroupId { get; set; }
    public string? SelectedGroupName { get; set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var (id, name) = await _storage.LoadAsync();
        SelectedGroupId = id;
        SelectedGroupName = name;
    }

    public async Task SelectGroupAsync(Guid? groupId, string? name)
    {
        SelectedGroupId = groupId;
        SelectedGroupName = name;
        await _storage.SaveAsync(groupId, name);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
