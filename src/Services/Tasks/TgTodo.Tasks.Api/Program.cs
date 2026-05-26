using Serilog;
using TgTodo.Tasks.Api.Middleware;
using TgTodo.Tasks.Application;
using TgTodo.Tasks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTasksApplication();
builder.Services.AddTasksInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.MigrateTasksDatabaseAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "tasks" }));

app.Run();
