using System.Security.Claims;
using System.Text.Json;

namespace TgTodo.Bff.Auth;

public static class AdminOidcConfig
{
    public static bool IsEnabled(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(Authority(config));

    public static string? Authority(IConfiguration config) =>
        config["Admin:Oidc:Authority"] ?? config["Admin__Oidc__Authority"];

    public static string? MetadataAddress(IConfiguration config) =>
        config["Admin:Oidc:MetadataAddress"] ?? config["Admin__Oidc__MetadataAddress"];

    public static string? JwksUri(IConfiguration config)
    {
        var jwks = config["Admin:Oidc:JwksUri"] ?? config["Admin__Oidc__JwksUri"];
        if (!string.IsNullOrWhiteSpace(jwks))
            return jwks;

        var metadataBase = MetadataAddress(config);
        return string.IsNullOrWhiteSpace(metadataBase)
            ? null
            : $"{metadataBase.TrimEnd('/')}/protocol/openid-connect/certs";
    }

    public static string? Audience(IConfiguration config) =>
        config["Admin:Oidc:Audience"] ?? config["Admin__Oidc__Audience"];

    public static string? ClientId(IConfiguration config) =>
        config["Admin:Oidc:ClientId"] ?? config["Admin__Oidc__ClientId"];

    public static string Scope(IConfiguration config) =>
        config["Admin:Oidc:Scope"] ?? config["Admin__Oidc__Scope"] ?? "openid profile email";

    public static string? RequiredRole(IConfiguration config) =>
        config["Admin:Oidc:RequiredRole"] ?? config["Admin__Oidc__RequiredRole"];

    public static bool SecretKeyAuthEnabled(IConfiguration config)
    {
        var key = config["Admin:SecretKey"] ?? config["Admin__SecretKey"];
        return !string.IsNullOrEmpty(key);
    }

    public static bool HasRequiredRole(ClaimsPrincipal user, string? requiredRole)
    {
        if (string.IsNullOrWhiteSpace(requiredRole))
            return true;

        if (user.IsInRole(requiredRole))
            return true;

        if (user.Claims.Any(c =>
                c.Type is "roles" or "role" or ClaimTypes.Role or "groups" &&
                string.Equals(c.Value, requiredRole, StringComparison.OrdinalIgnoreCase)))
            return true;

        return HasKeycloakRole(user, requiredRole);
    }

    private static bool HasKeycloakRole(ClaimsPrincipal user, string requiredRole)
    {
        foreach (var claim in user.Claims.Where(c => c.Type is "realm_access" or "resource_access"))
        {
            try
            {
                using var doc = JsonDocument.Parse(claim.Value);
                if (ContainsRole(doc.RootElement, requiredRole))
                    return true;
            }
            catch (JsonException)
            {
                // ignore malformed claim payloads
            }
        }

        return false;
    }

    private static bool ContainsRole(JsonElement element, string requiredRole)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("roles") && property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in property.Value.EnumerateArray())
                    {
                        if (role.ValueKind == JsonValueKind.String &&
                            string.Equals(role.GetString(), requiredRole, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }

                if (ContainsRole(property.Value, requiredRole))
                    return true;
            }
        }

        return false;
    }
}

public record AdminAuthConfigDto(
    bool SecretKeyAuthEnabled,
    bool OidcEnabled,
    string? Issuer,
    string? ClientId,
    string Scope,
    string? RequiredRole);
