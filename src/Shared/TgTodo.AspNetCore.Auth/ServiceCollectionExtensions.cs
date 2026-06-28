using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace TgTodo.AspNetCore.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTgTodoUserAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthentication(TgTodoAuthDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, UserIdHeaderAuthenticationHandler>(
                TgTodoAuthDefaults.SchemeName,
                _ => { });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(TgTodoAuthDefaults.SchemeName)
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
