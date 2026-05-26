using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TgTodo.Identity.Application.Abstractions;
using TgTodo.Identity.Infrastructure.Persistence;
using TgTodo.Identity.Infrastructure.Repositories;

namespace TgTodo.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Identity"), npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(3), null)));

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }

    public static async Task MigrateIdentityDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
