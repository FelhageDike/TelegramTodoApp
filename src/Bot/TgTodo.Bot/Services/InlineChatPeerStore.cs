using System.Collections.Concurrent;

namespace TgTodo.Bot.Services;

/// <summary>
/// Запоминает telegram id пользователей, которые взаимодействовали с ботом в одном чате (по <c>chat_instance</c>).
/// Нужно, чтобы в личке понять «собеседника» и показать кнопку общей группы TgTodo.
/// </summary>
public sealed class InlineChatPeerStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, byte>> _peers = new();

    public void Remember(string? chatInstance, string? draftId, long telegramUserId)
    {
        if (telegramUserId == 0)
            return;

        RememberKey(chatInstance, telegramUserId);
        if (!string.IsNullOrWhiteSpace(draftId))
            RememberKey(DraftKey(draftId), telegramUserId);
    }

    /// <summary>Единственный собеседник в личке (ровно один другой id в чате).</summary>
    public long? TryGetSingleOtherPeer(string? chatInstance, string? draftId, long authorTelegramId)
    {
        var other = TryGetSingleOtherPeer(chatInstance, authorTelegramId);
        if (other is not null)
            return other;

        if (string.IsNullOrWhiteSpace(draftId))
            return null;

        return TryGetSingleOtherPeer(DraftKey(draftId), authorTelegramId);
    }

    private static string DraftKey(string draftId) => $"draft:{draftId}";

    private void RememberKey(string? key, long telegramUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var bucket = _peers.GetOrAdd(key, _ => new ConcurrentDictionary<long, byte>());
        bucket.TryAdd(telegramUserId, 0);
    }

    private long? TryGetSingleOtherPeer(string? key, long authorTelegramId)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (!_peers.TryGetValue(key, out var bucket))
            return null;

        var others = bucket.Keys.Where(id => id != authorTelegramId).ToList();
        return others.Count == 1 ? others[0] : null;
    }
}
