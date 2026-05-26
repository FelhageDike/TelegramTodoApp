using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TgTodo.Groups.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddGroupsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
