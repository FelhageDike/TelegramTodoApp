using System.Collections.Concurrent;

namespace TgTodo.Bot.Services;

public sealed class UserSession
{
    public Guid? SelectedGroupId { get; set; }
    public string? SelectedGroupName { get; set; }
    public string? PendingCommand { get; set; }
}

public sealed class UserSessionStore
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    public UserSession GetOrCreate(long telegramId) =>
        _sessions.GetOrAdd(telegramId, _ => new UserSession());

    public void SetGroup(long telegramId, Guid? groupId, string? groupName)
    {
        var session = GetOrCreate(telegramId);
        session.SelectedGroupId = groupId;
        session.SelectedGroupName = groupName;
        session.PendingCommand = null;
    }

    public void SetPending(long telegramId, string command)
    {
        var session = GetOrCreate(telegramId);
        session.PendingCommand = command;
    }

    public void ClearPending(long telegramId)
    {
        var session = GetOrCreate(telegramId);
        session.PendingCommand = null;
    }
}
