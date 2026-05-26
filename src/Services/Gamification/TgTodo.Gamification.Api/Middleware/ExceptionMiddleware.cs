using System.Net;
using System.Text.Json;
using TgTodo.BuildingBlocks.Exceptions;

namespace TgTodo.Gamification.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, message) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ForbiddenException => (HttpStatusCode.Forbidden, ex.Message),
                ConflictException => (HttpStatusCode.Conflict, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An error occurred.")
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
