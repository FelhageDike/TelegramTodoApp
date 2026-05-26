using System.Net.Http.Json;
using System.Text.Json;
using TgTodo.Contracts.Enums;

namespace TgTodo.MiniApp.Services;

public record GroupDto(Guid Id, string Name, string InviteCode, GroupRole MyRole);
public record TaskDto(
    Guid Id,
    TaskScope Scope,
    Guid? GroupId,
    string Title,
    int PointsReward,
    RecurrenceType Recurrence,
    PersonalTaskVisibility PersonalVisibility,
    Guid? CategoryId,
    Guid? AssignedToUserId,
    Guid CreatedByUserId,
    bool IsCompletedForPeriod);
public record CategoryDto(Guid Id, string Name, string? Emoji, Guid? GroupId);
public record BalanceDto(int PersonalBalance, int? GroupBalance);
public record LedgerEntryDto(int Delta, string Reason, Guid? ReferenceId, DateTime CreatedAt);
public record GroupMemberViewDto(Guid UserId, string DisplayName, GroupRole Role, DateTime JoinedAt);
public record HomeDto(
    Guid UserId,
    BalanceDto Balance,
    List<TaskDto> Tasks,
    List<GroupDto> Groups,
    List<GroupMemberViewDto>? Members);

public record DayTasksDto(DateOnly Date, List<TaskDto> Tasks);

public record MonthTasksDto(int Year, int Month, List<DayTasksDto> Days);

public class BffApiClient
{
    private readonly HttpClient _http;
    private readonly TelegramInterop _telegram;

    public BffApiClient(HttpClient http, TelegramInterop telegram)
    {
        _http = http;
        _telegram = telegram;
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var initData = await _telegram.GetInitDataAsync();
        if (!string.IsNullOrEmpty(initData))
            request.Headers.TryAddWithoutValidation("Authorization", $"tma {initData}");
        else
            request.Headers.TryAddWithoutValidation("X-Dev-Telegram-Id", "100000001");
        return request;
    }

    public async Task<HomeDto?> GetHomeAsync(Guid? groupId, DateOnly? date = null)
    {
        var parts = new List<string>();
        if (groupId.HasValue) parts.Add($"groupId={groupId}");
        if (date.HasValue) parts.Add($"date={date:yyyy-MM-dd}");
        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        var request = await CreateRequestAsync(HttpMethod.Get, $"bff/home{query}");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<HomeDto>();
    }

    public async Task<MonthTasksDto?> GetMonthTasksAsync(Guid? groupId, int year, int month)
    {
        var parts = new List<string> { $"year={year}", $"month={month}" };
        if (groupId.HasValue) parts.Add($"groupId={groupId}");
        var query = "?" + string.Join("&", parts);
        var request = await CreateRequestAsync(HttpMethod.Get, $"bff/home/month{query}");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MonthTasksDto>();
    }

    public async Task<List<GroupDto>?> GetGroupsAsync()
    {
        var request = await CreateRequestAsync(HttpMethod.Get, "bff/groups");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<GroupDto>>();
    }

    public async Task<(GroupDto? Group, string? Error)> CreateGroupAsync(string name)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, "bff/groups",
            JsonContent.Create(new { Name = name }));
        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<GroupDto>(), null);
        return (null, await ReadErrorAsync(response));
    }

    public async Task<(bool Success, string? Error)> LeaveGroupAsync(Guid groupId)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, $"bff/groups/{groupId}/leave");
        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return (true, null);
        return (false, await ReadErrorAsync(response));
    }

    public async Task<(bool Success, string? Error)> DeleteGroupAsync(Guid groupId)
    {
        var request = await CreateRequestAsync(HttpMethod.Delete, $"bff/groups/{groupId}");
        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return (true, null);
        return (false, await ReadErrorAsync(response));
    }

    public async Task<(GroupDto? Group, string? Error)> JoinGroupAsync(string inviteCode)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, "bff/groups/join",
            JsonContent.Create(new { InviteCode = inviteCode }));
        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<GroupDto>(), null);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (null, "Не удалось войти. Откройте приложение через кнопку в боте Telegram (не в обычном браузере).");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return (null, "Группа с таким кодом не найдена. Проверьте код или спросите новый.");
        return (null, await ReadErrorAsync(response));
    }

    public async Task<(TaskDto? Task, string? Error)> CreateTaskAsync(object body)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, "bff/tasks", JsonContent.Create(body));
        var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<TaskDto>(), null);
        return (null, await ReadErrorAsync(response));
    }

    public async Task<TaskDto?> CompleteTaskAsync(Guid taskId, DateOnly? date = null)
    {
        var query = date.HasValue ? $"?date={date:yyyy-MM-dd}" : string.Empty;
        var request = await CreateRequestAsync(HttpMethod.Post, $"bff/tasks/{taskId}/complete{query}");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task<BalanceDto?> GetBalanceAsync(Guid? groupId)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}" : string.Empty;
        var request = await CreateRequestAsync(HttpMethod.Get, $"bff/balance{query}");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BalanceDto>();
    }

    public async Task<List<GroupMemberViewDto>?> GetGroupMembersAsync(Guid groupId)
    {
        var request = await CreateRequestAsync(HttpMethod.Get, $"bff/groups/{groupId}/members");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<GroupMemberViewDto>>();
    }

    public async Task<List<LedgerEntryDto>?> GetLedgerAsync(Guid? groupId, int take = 50)
    {
        var parts = new List<string> { $"take={take}" };
        if (groupId.HasValue) parts.Add($"groupId={groupId}");
        var query = "?" + string.Join("&", parts);
        var request = await CreateRequestAsync(HttpMethod.Get, $"bff/ledger{query}");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<List<LedgerEntryDto>>();
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("error", out var err) && err.GetString() is { } text)
                return text;
        }
        catch
        {
            // ignore
        }

        return $"Ошибка ({(int)response.StatusCode})";
    }
}
