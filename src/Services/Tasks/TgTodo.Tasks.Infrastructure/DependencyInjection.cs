using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Infrastructure.Clients;
using TgTodo.Tasks.Infrastructure.Outbox;
using TgTodo.Tasks.Infrastructure.Persistence;
using TgTodo.Tasks.Infrastructure.Repositories;

namespace TgTodo.Tasks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTasksInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TasksDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Tasks"), npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null)));

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddHttpClient<IGroupsClient, GroupsHttpClient>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMQ:Host"] ?? "localhost",
                    h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "tgtodo");
                        h.Password(configuration["RabbitMQ:Password"] ?? "tgtodo");
                    });
            });
        });

        services.AddHostedService<OutboxDispatcher>();
        return services;
    }

    public static async Task MigrateTasksDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TasksDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
