using Microsoft.AspNetCore.Builder;

namespace TgTodo.AspNetCore.Auth;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseTgTodoUserAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
