using System.Collections.Concurrent;

namespace TgTodo.Bot.Services;

/// <summary>Черновики inline-карточек, у которых клавиатура уже обновлена для собеседника.</summary>
public sealed class InlineDraftKeyboardStore
{
    private readonly ConcurrentDictionary<string, byte> _enriched = new();

    public bool IsEnriched(string draftId) => _enriched.ContainsKey(draftId);

    public void MarkEnriched(string draftId) => _enriched.TryAdd(draftId, 0);

    public void Forget(string draftId) => _enriched.TryRemove(draftId, out _);
}
