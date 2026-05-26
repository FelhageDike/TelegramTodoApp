using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TgTodo.Bot;
using TgTodo.Contracts.Enums;

namespace TgTodo.Bot.Services;

public sealed class BffClient
{
    private readonly HttpClient _http;
    private readonly BotOptions _options;

    public BffClient(HttpClient http, IOptions<BotOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BffBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<HomeDto?> GetHomeAsync(long telegramId, string displayName, Guid? groupId, DateOnly? date, CancellationToken ct)
    {
        var parts = new List<string>();
        if (groupId.HasValue) parts.Add($"groupId={groupId}");
        if (date.HasValue) parts.Add($"date={date:yyyy-MM-dd}");
        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        var response = await SendAsync(telegramId, displayName, HttpMethod.Get, $"bff/home{query}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<HomeDto>(cancellationToken: ct);
    }

    public async Task<BalanceDto?> GetBalanceAsync(long telegramId, string displayName, Guid? groupId, CancellationToken ct)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}" : "";
        var response = await SendAsync(telegramId, displayName, HttpMethod.Get, $"bff/balance{query}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BalanceDto>(cancellationToken: ct);
    }

    public async Task<List<LedgerEntryDto>?> GetLedgerAsync(long telegramId, string displayName, Guid? groupId, int take, CancellationToken ct)
    {
        var parts = new List<string> { $"take={take}" };
        if (groupId.HasValue) parts.Add($"groupId={groupId}");
        var response = await SendAsync(telegramId, displayName, HttpMethod.Get, $"bff/ledger?{string.Join("&", parts)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<LedgerEntryDto>>(cancellationToken: ct);
    }

    public async Task<List<GroupDto>?> GetGroupsAsync(long telegramId, string displayName, CancellationToken ct)
    {
        var response = await SendAsync(telegramId, displayName, HttpMethod.Get, "bff/groups", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<GroupDto>>(cancellationToken: ct);
    }

    public async Task<(GroupDto? Group, string? Error)> CreateGroupAsync(long telegramId, string displayName, string name, CancellationToken ct)
    {
        var response = await SendAsync(telegramId, displayName, HttpMethod.Post, "bff/groups",
            JsonContent.Create(new { name }), ct);
        return await ReadResultAsync<GroupDto>(response, ct);
    }

    public async Task<(GroupDto? Group, string? Error)> JoinGroupAsync(long telegramId, string displayName, string inviteCode, CancellationToken ct)
    {
        var response = await SendAsync(telegramId, displayName, HttpMethod.Post, "bff/groups/join",
            JsonContent.Create(new { inviteCode }), ct);
        return await ReadResultAsync<GroupDto>(response, ct);
    }

    public async Task<(TaskDto? Task, string? Error)> CreateTaskAsync(long telegramId, string displayName, object body, CancellationToken ct)
    {
        var response = await SendAsync(telegramId, displayName, HttpMethod.Post, "bff/tasks",
            JsonContent.Create(body), ct);
        return await ReadResultAsync<TaskDto>(response, ct);
    }

    public async Task<(TaskDto? Task, string? Error)> CompleteTaskAsync(long telegramId, string displayName, Guid taskId, DateOnly? date, CancellationToken ct)
    {
        var query = date.HasValue ? $"?date={date:yyyy-MM-dd}" : "";
        var response = await SendAsync(telegramId, displayName, HttpMethod.Post, $"bff/tasks/{taskId}/complete{query}", null, ct);
        return await ReadResultAsync<TaskDto>(response, ct);
    }

    private Task<HttpResponseMessage> SendAsync(long telegramId, string displayName, HttpMethod method, string url, CancellationToken ct) =>
        SendAsync(telegramId, displayName, method, url, null, ct);

    private Task<HttpResponseMessage> SendAsync(long telegramId, string displayName, HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.TryAddWithoutValidation("X-TgTodo-Bot-Key", _options.InternalKey);
        request.Headers.TryAddWithoutValidation("X-Telegram-User-Id", telegramId.ToString());
        request.Headers.TryAddWithoutValidation("X-Telegram-Display-Name", EncodeDisplayName(displayName));
        return _http.SendAsync(request, ct);
    }

    /// <summary>HTTP headers allow ASCII only; Telegram names may contain Cyrillic.</summary>
    private static string EncodeDisplayName(string displayName) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(displayName));

    private static async Task<(T? Value, string? Error)> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct), null);

        var message = "Ошибка запроса";
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (json.TryGetProperty("error", out var err) && err.GetString() is { } text)
                message = text;
        }
        catch
        {
            // ignore
        }

        return (default, message);
    }

    public object BuildCreateTaskBody(string title, int points, Guid? groupId, DateOnly startDate) => new
    {
        scope = groupId.HasValue ? TaskScope.Group : TaskScope.Personal,
        title,
        pointsReward = points,
        recurrence = RecurrenceType.None,
        weekday = (int?)null,
        dayOfMonth = (int?)null,
        intervalDays = (int?)null,
        personalVisibility = PersonalTaskVisibility.Private,
        completionMode = CompletionMode.AnyMember,
        groupId,
        assignedToUserId = (Guid?)null,
        categoryId = (Guid?)null,
        visibilityGroupId = (Guid?)null,
        startDate = startDate.ToString("yyyy-MM-dd")
    };
}
