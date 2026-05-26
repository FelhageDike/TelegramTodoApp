using Serilog;
using TgTodo.Groups.Api.Middleware;
using TgTodo.Groups.Application;
using TgTodo.Groups.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGroupsApplication();
builder.Services.AddGroupsInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.Services.MigrateGroupsDatabaseAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "groups" }));

app.Run();
