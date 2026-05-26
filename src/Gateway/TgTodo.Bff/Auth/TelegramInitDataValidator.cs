using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace TgTodo.Bff.Auth;

public class TelegramInitDataValidator
{
    private readonly string _botToken;

    public TelegramInitDataValidator(IConfiguration configuration)
    {
        _botToken = configuration["BOT_TOKEN"] ?? configuration["BotToken"] ?? string.Empty;
    }

    public bool TryValidate(string initData, out TelegramUser? user)
    {
        user = null;
        if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(initData))
            return false;

        var parameters = QueryHelpers.ParseQuery(initData);
        if (!parameters.TryGetValue("hash", out var hashValues))
            return false;

        var receivedHash = hashValues.ToString();
        if (string.IsNullOrEmpty(receivedHash))
            return false;

        var dataCheckPairs = parameters
            .Where(p => !string.Equals(p.Key, "hash", StringComparison.Ordinal))
            .Select(p => $"{p.Key}={p.Value}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var dataCheckString = string.Join('\n', dataCheckPairs);
        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(_botToken));

        var computedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();

        if (!string.Equals(computedHex, receivedHash, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!parameters.TryGetValue("user", out var userValues))
            return false;

        var userJson = userValues.ToString();
        if (string.IsNullOrEmpty(userJson))
            return false;

        user = System.Text.Json.JsonSerializer.Deserialize<TelegramUser>(userJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return user is not null;
    }
}

public record TelegramUser(long Id, string? FirstName, string? LastName, string? Username);
