using System.Collections.Concurrent;
using TgTodo.Contracts.Enums;

namespace TgTodo.Bot.Services;

public sealed class InlineTaskDraft
{
    public required string Id { get; init; }
    public long TelegramUserId { get; init; }
    public required string Title { get; init; }
    public int Points { get; init; }
    public Guid? GroupId { get; init; }
    public TaskScope Scope { get; init; }
    public required string ScopeLabel { get; init; }
    public DateOnly StartDate { get; init; }
    public DateTime ExpiresAt { get; init; }
}

public sealed class InlineDraftStore
{
    private readonly ConcurrentDictionary<string, InlineTaskDraft> _drafts = new();

    public string Save(InlineTaskDraft draft)
    {
        _drafts[draft.Id] = draft;
        return draft.Id;
    }

    public InlineTaskDraft? Get(string id)
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
