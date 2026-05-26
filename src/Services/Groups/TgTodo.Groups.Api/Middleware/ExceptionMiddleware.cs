using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TgTodo.BuildingBlocks.Exceptions;

namespace TgTodo.Groups.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var (status, message) = ex switch
            {
                ValidationException => (HttpStatusCode.BadRequest, ex.Message),
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ForbiddenException => (HttpStatusCode.Forbidden, ex.Message),
                ConflictException => (HttpStatusCode.Conflict, ex.Message),
                DbUpdateException db => (HttpStatusCode.Conflict, ResolveDbMessage(db)),
                _ => (HttpStatusCode.InternalServerError,
                    _env.IsDevelopment() ? ex.Message : "An error occurred.")
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }

    private static string ResolveDbMessage(DbUpdateException ex) =>
        ex.InnerException?.Message ?? ex.Message;
}
