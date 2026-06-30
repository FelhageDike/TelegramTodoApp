using System.Net.Http.Json;
using TgTodo.BuildingBlocks.Abstractions;

namespace TgTodo.AspNetCore.Auth.Clients;

public sealed class GroupsMembershipHttpClient : IGroupsMembershipClient, IGroupMembershipChecker
{
    private readonly HttpClient _http;

    public GroupsMembershipHttpClient(HttpClient http) => _http = http;

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/groups/{groupId}/membership");
        request.Headers.Add(TgTodoAuthDefaults.UserIdHeaderName, userId.ToString());
        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
    }
}
