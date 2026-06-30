using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using TgTodo.AspNetCore.Auth.Clients;
using TgTodo.BuildingBlocks.Abstractions;

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

    public static IServiceCollection AddTgTodoRemoteGroupMembership(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<IGroupsMembershipClient, GroupsMembershipHttpClient>((_, client) =>
        {
            if (configuration["Services:Groups"] is { } baseUrl)
                client.BaseAddress = new Uri(baseUrl);
        })
        .AddTgTodoServiceAuth();

        services.AddScoped<IGroupMembershipChecker>(sp =>
            (IGroupMembershipChecker)sp.GetRequiredService<IGroupsMembershipClient>());

        return services;
    }

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

    public static IServiceCollection AddTgTodoGroupAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, GroupMemberAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(TgTodoAuthDefaults.GroupMemberPolicy, policy =>
                policy
                    .AddAuthenticationSchemes(TgTodoAuthDefaults.UserSchemeName)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new GroupMemberRequirement { AllowMissingGroupId = false }));

            options.AddPolicy(TgTodoAuthDefaults.GroupMemberOptionalPolicy, policy =>
                policy
                    .AddAuthenticationSchemes(TgTodoAuthDefaults.UserSchemeName)
                    .RequireAuthenticatedUser()
                    .AddRequirements(new GroupMemberRequirement { AllowMissingGroupId = true }));
        });

        return services;
    }
}

