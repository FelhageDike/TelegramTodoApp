using Microsoft.JSInterop;

namespace TgTodo.MiniApp.Services;

public class GroupSelectionStorage
{
    private readonly IJSRuntime _js;

    public GroupSelectionStorage(IJSRuntime js) => _js = js;

    public async Task<(Guid? Id, string? Name)> LoadAsync()
    {
        try
        {
            var result = await _js.InvokeAsync<StoredGroup?>("tgTodoAppState.getSelectedGroup");
            if (result?.Id is null || !Guid.TryParse(result.Id, out var id))
                return (null, null);
            return (id, result.Name);
        }
        catch
        {
            return (null, null);
        }
    }

    public async Task SaveAsync(Guid? groupId, string? groupName)
    {
        try
        {
            await _js.InvokeVoidAsync("tgTodoAppState.setSelectedGroup",
                groupId?.ToString(),
                groupName ?? string.Empty);
        }
        catch
        {
            // ignore when JS not ready
        }
    }

    private sealed class StoredGroup
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
