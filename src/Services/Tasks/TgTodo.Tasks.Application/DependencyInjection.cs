using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TgTodo.Tasks.Application.Services;

namespace TgTodo.Tasks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTasksApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<TaskAccessService>();
        return services;
    }
}
