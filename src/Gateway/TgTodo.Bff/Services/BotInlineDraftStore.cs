using System.Collections.Concurrent;
using TgTodo.Contracts.Bot;

namespace TgTodo.Bff.Services;

public sealed class BotInlineDraftStore
{
    private readonly ConcurrentDictionary<string, InlineTaskDraftDto> _drafts = new();

    public void Upsert(InlineTaskDraftDto draft) => _drafts[draft.Id] = draft;

    public InlineTaskDraftDto? Get(string id)
    {
        if (!_drafts.TryGetValue(id, out var draft))
            return null;
        if (draft.ExpiresAt < DateTime.UtcNow)
        {
            _drafts.TryRemove(id, out _);
            return null;
        }

        return draft;
    }

    public void Delete(string id) => _drafts.TryRemove(id, out _);

    public void CleanupExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _drafts.Keys)
        {
            if (_drafts.TryGetValue(key, out var d) && d.ExpiresAt < now)
                _drafts.TryRemove(key, out _);
        }
    }
}
