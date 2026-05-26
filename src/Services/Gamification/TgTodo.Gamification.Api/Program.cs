using Serilog;
using TgTodo.Gamification.Api.Middleware;
using TgTodo.Gamification.Application;
using TgTodo.Gamification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGamificationApplication();
builder.Services.AddGamificationInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await app.Services.MigrateGamificationDatabaseAsync();
}

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gamification" }));

app.Run();
