using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace TgTodo.Bff.Auth;

public static class AdminAuthDefaults
{
    public const string AdminKeyHeader = "X-Admin-Key";
    public const string OidcScheme = "AdminOidc";
}

public static class AdminAuthExtensions
{
    public static RouteHandlerBuilder RequireAdminKey(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<AdminAuthFilter>();
}

public sealed class AdminAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (!AdminOidcConfig.SecretKeyAuthEnabled(config) && !AdminOidcConfig.IsEnabled(config))
            return Results.NotFound(new { error = "Admin dashboard is not configured." });

        if (await TryValidateOidcAsync(context.HttpContext, config))
            return await next(context);

        var expectedKey = config["Admin:SecretKey"] ?? config["Admin__SecretKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return Results.Json(new { error = "Unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);

        if (!context.HttpContext.Request.Headers.TryGetValue(AdminAuthDefaults.AdminKeyHeader, out var key) ||
            key != expectedKey)
        {
            return Results.Json(new { error = "Unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }

    private static async Task<bool> TryValidateOidcAsync(HttpContext context, IConfiguration config)
    {
        if (!AdminOidcConfig.IsEnabled(config))
            return false;

        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            return false;

        var value = authHeader.ToString();
        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var authResult = await context.AuthenticateAsync(AdminAuthDefaults.OidcScheme);
        if (authResult.Succeeded && authResult.Principal is not null)
            context.User = authResult.Principal;

        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        return AdminOidcConfig.HasRequiredRole(context.User, AdminOidcConfig.RequiredRole(config));
    }
}

public static class AdminOidcExtensions
{
    public static IServiceCollection AddAdminOidcAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!AdminOidcConfig.IsEnabled(configuration))
            return services;

        var authority = AdminOidcConfig.Authority(configuration)!;
        var audience = AdminOidcConfig.Audience(configuration);
        var jwksUri = AdminOidcConfig.JwksUri(configuration);
        var useInternalJwks = !string.IsNullOrWhiteSpace(jwksUri) &&
                              !string.IsNullOrWhiteSpace(AdminOidcConfig.MetadataAddress(configuration));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AdminAuthDefaults.OidcScheme;
                options.DefaultChallengeScheme = AdminAuthDefaults.OidcScheme;
            })
            .AddJwtBearer(AdminAuthDefaults.OidcScheme, options =>
            {
                options.TokenValidationParameters.ValidateAudience = !string.IsNullOrEmpty(audience);
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.ValidIssuer = authority;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata =
                    authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(audience))
                    options.Audience = audience;

                if (useInternalJwks)
                {
                    var requireHttps = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                    options.TokenValidationParameters.IssuerSigningKeys =
                        AdminJwksLoader.LoadSigningKeys(jwksUri!, requireHttps);
                }
                else
                {
                    options.Authority = authority;
                }
            });

        services.AddAuthorization();

        return services;
    }
}
