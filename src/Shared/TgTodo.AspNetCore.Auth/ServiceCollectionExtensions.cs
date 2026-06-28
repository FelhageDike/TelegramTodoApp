using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TgTodo.AspNetCore.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTgTodoServiceAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SectionName));
        services.AddTransient<ServiceAuthDelegatingHandler>();
        return services;
    }

    public static IHttpClientBuilder AddTgTodoServiceAuth(this IHttpClientBuilder builder) =>
        builder.AddHttpMessageHandler<ServiceAuthDelegatingHandler>();

    public static IServiceCollection AddTgTodoUserAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTgTodoServiceAuth(configuration);

        services
            .AddAuthentication(TgTodoAuthDefaults.UserSchemeName)
            .AddScheme<AuthenticationSchemeOptions, UserIdHeaderAuthenticationHandler>(
                TgTodoAuthDefaults.UserSchemeName,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, InternalServiceAuthenticationHandler>(
                TgTodoAuthDefaults.InternalSchemeName,
                _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(TgTodoAuthDefaults.InternalPolicy, policy =>
                policy
                    .AddAuthenticationSchemes(TgTodoAuthDefaults.InternalSchemeName)
                    .RequireAuthenticatedUser());

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(TgTodoAuthDefaults.UserSchemeName)
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
