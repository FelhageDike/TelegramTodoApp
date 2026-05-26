using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TgTodo.Contracts.Enums;

namespace TgTodo.Bff.Clients;

public record IdentityUserDto(Guid Id, long TelegramId, string DisplayName, string Timezone);

public class IdentityApiClient
{
    private readonly HttpClient _http;

    public IdentityApiClient(HttpClient http) => _http = http;

    public async Task<IdentityUserDto> EnsureUserAsync(long telegramId, string displayName, string timezone)
    {
        var response = await _http.PostAsJsonAsync("internal/users/ensure",
            new { TelegramId = telegramId, DisplayName = displayName, Timezone = timezone });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdentityUserDto>())!;
    }

    public async Task<IReadOnlyList<IdentityUserDto>> GetUsersByIdsAsync(IReadOnlyList<Guid> userIds)
    {
        var response = await _http.PostAsJsonAsync("internal/users/by-ids", new { UserIds = userIds });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<IdentityUserDto>>())!;
    }

    public async Task<IdentityUserDto?> GetUserAsync(Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/users/me");
        request.Headers.Add("X-User-Id", userId.ToString());
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<IdentityUserDto>();
    }

    public async Task<IdentityUserDto> UpdateTimezoneAsync(Guid userId, string timezone)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "api/users/me/timezone")
        {
            Content = JsonContent.Create(new { Timezone = timezone })
        };
        request.Headers.Add("X-User-Id", userId.ToString());
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdentityUserDto>())!;
    }
}

public record GroupDto(Guid Id, string Name, string InviteCode, GroupRole MyRole);
public record GroupMemberDto(Guid UserId, GroupRole Role, DateTime JoinedAt);

public class GroupsApiClient
{
    private readonly HttpClient _http;

    public GroupsApiClient(HttpClient http) => _http = http;

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, Guid userId)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-User-Id", userId.ToString());
        return request;
    }

    public async Task<IReadOnlyList<GroupDto>> GetGroupsAsync(Guid userId)
    {
        var request = CreateRequest(HttpMethod.Get, "api/groups", userId);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<GroupDto>>())!;
    }

    public async Task<GroupDto> CreateGroupAsync(Guid userId, string name)
    {
        var request = CreateRequest(HttpMethod.Post, "api/groups", userId);
        request.Content = JsonContent.Create(new { Name = name });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw await ApiClientException.FromResponseAsync(response);
        return (await response.Content.ReadFromJsonAsync<GroupDto>())!;
    }

    public async Task LeaveGroupAsync(Guid userId, Guid groupId)
    {
        var request = CreateRequest(HttpMethod.Post, $"api/groups/{groupId}/leave", userId);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw await ApiClientException.FromResponseAsync(response);
    }

    public async Task DeleteGroupAsync(Guid userId, Guid groupId)
    {
        var request = CreateRequest(HttpMethod.Delete, $"api/groups/{groupId}", userId);
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw await ApiClientException.FromResponseAsync(response);
    }

    public async Task<GroupDto> JoinGroupAsync(Guid userId, string inviteCode)
    {
        var request = CreateRequest(HttpMethod.Post, "api/groups/join", userId);
        request.Content = JsonContent.Create(new { InviteCode = inviteCode });
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw await ApiClientException.FromResponseAsync(response);
        return (await response.Content.ReadFromJsonAsync<GroupDto>())!;
    }

    public async Task<IReadOnlyList<GroupMemberDto>> GetMembersAsync(Guid userId, Guid groupId)
    {
        var request = CreateRequest(HttpMethod.Get, $"api/groups/{groupId}/members", userId);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<GroupMemberDto>>())!;
    }
}

public class ApiClientException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiClientException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public static async Task<ApiClientException> FromResponseAsync(HttpResponseMessage response)
    {
        var message = "An error occurred.";
        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("error", out var err) && err.GetString() is { } text)
                message = text;
        }
        catch
        {
            // ignore parse errors
        }

        return new ApiClientException(message, response.StatusCode);
    }
}

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
    IReadOnlyList<TaskDto> Tasks,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<GroupMemberViewDto>? Members);

public record DayTasksDto(DateOnly Date, IReadOnlyList<TaskDto> Tasks);

public record MonthTasksDto(int Year, int Month, IReadOnlyList<DayTasksDto> Days);

public class TasksApiClient
{
    private readonly HttpClient _http;

    public TasksApiClient(HttpClient http) => _http = http;

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, Guid userId, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Add("X-User-Id", userId.ToString());
        return request;
    }

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid userId, Guid? groupId, DateOnly? date)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}" : string.Empty;
        if (date.HasValue) query += (query.Length > 0 ? "&" : "?") + $"date={date:yyyy-MM-dd}";
        var request = CreateRequest(HttpMethod.Get, $"api/tasks{query}", userId);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<TaskDto>>())!;
    }

    public async Task<MonthTasksDto> GetMonthTasksAsync(Guid userId, Guid? groupId, int year, int month)
    {
        var parts = new List<string> { $"year={year}", $"month={month}" };
        if (groupId.HasValue) parts.Add($"groupId={groupId}");
        var request = CreateRequest(HttpMethod.Get, $"api/tasks/month?{string.Join("&", parts)}", userId);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MonthTasksDto>())!;
    }

    public async Task<TaskDto> CreateTaskAsync(Guid userId, object body)
    {
        var request = CreateRequest(HttpMethod.Post, "api/tasks", userId, JsonContent.Create(body));
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw await ApiClientException.FromResponseAsync(response);
        return (await response.Content.ReadFromJsonAsync<TaskDto>())!;
    }

    public async Task<TaskDto> CompleteTaskAsync(Guid userId, Guid taskId, DateOnly? date = null)
    {
        var query = date.HasValue ? $"?date={date:yyyy-MM-dd}" : string.Empty;
        var request = CreateRequest(HttpMethod.Post, $"api/tasks/{taskId}/complete{query}", userId);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDto>())!;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid userId, Guid? groupId)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}" : string.Empty;
        var request = CreateRequest(HttpMethod.Get, $"api/categories{query}", userId);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<CategoryDto>>())!;
    }
}

public class GamificationApiClient
{
    private readonly HttpClient _http;

    public GamificationApiClient(HttpClient http) => _http = http;

    public async Task<BalanceDto> GetBalanceAsync(Guid userId, Guid? groupId)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}" : string.Empty;
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/balance{query}");
        request.Headers.Add("X-User-Id", userId.ToString());
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BalanceDto>())!;
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(Guid userId, Guid? groupId, int take = 50)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}&take={take}" : $"?take={take}";
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/ledger{query}");
        request.Headers.Add("X-User-Id", userId.ToString());
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<LedgerEntryDto>>())!;
    }
}
