using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgTodo.Groups.Application.Abstractions;
using TgTodo.Groups.Infrastructure.Persistence;
using TgTodo.Groups.Infrastructure.Repositories;

namespace TgTodo.Groups.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGroupsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GroupsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Groups"), npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null)));

        services.AddScoped<IGroupRepository, GroupRepository>();
        return services;
    }

    public static async Task MigrateGroupsDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GroupsDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
