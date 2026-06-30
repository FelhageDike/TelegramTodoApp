namespace TgTodo.AspNetCore.Auth;

public class ServiceAuthOptions
{
    public const string SectionName = "ServiceAuth";

    /// <summary>Shared key for service-to-service calls (internal endpoints).</summary>
    public string? InternalKey { get; set; }

    /// <summary>HMAC secret for signing X-User-Id on outbound calls from trusted services.</summary>
    public string? UserIdSigningKey { get; set; }

    /// <summary>When true and <see cref="InternalKey"/> is set, user API requires X-TgTodo-Service-Key.</summary>
    public bool RequireServiceKeyForUserApi { get; set; } = true;
}
