using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using TgTodo.Tasks.Application.Abstractions;

namespace TgTodo.Tasks.Infrastructure.Clients;

public class GroupsHttpClient : IGroupsClient
{
    private readonly HttpClient _http;

    public GroupsHttpClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        if (configuration["Services:Groups"] is { } baseUrl)
            _http.BaseAddress = new Uri(baseUrl);
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/groups/{groupId}/membership");
        request.Headers.Add("X-User-Id", userId.ToString());
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return false;
        return await response.Content.ReadFromJsonAsync<bool>(ct);
    }
}
