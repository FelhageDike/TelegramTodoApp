using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgTodo.Gamification.Application.Abstractions;
using TgTodo.Gamification.Application.Consumers;
using TgTodo.Gamification.Infrastructure.Persistence;
using TgTodo.Gamification.Infrastructure.Repositories;

namespace TgTodo.Gamification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGamificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GamificationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Gamification"), npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null)));

        services.AddScoped<IGamificationRepository, GamificationRepository>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<TaskCompletedConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMQ:Host"] ?? "localhost",
                    h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "tgtodo");
                        h.Password(configuration["RabbitMQ:Password"] ?? "tgtodo");
                    });
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    public static async Task MigrateGamificationDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
