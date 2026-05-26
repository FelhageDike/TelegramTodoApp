using System.Text.Json;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgTodo.Bot;
using TgTodo.Bot.Services;

var builder = WebApplication.CreateBuilder(args);

var botToken = builder.Configuration["BOT_TOKEN"] ?? builder.Configuration["Bot:Token"];
builder.Services.Configure<BotOptions>(options =>
{
    builder.Configuration.GetSection(BotOptions.SectionName).Bind(options);
    if (!string.IsNullOrWhiteSpace(botToken))
        options.Token = botToken;
});

builder.Services.AddHttpClient<BffClient>();
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var token = sp.GetRequiredService<IOptions<BotOptions>>().Value.Token;
    return new TelegramBotClient(token ?? "");
});

builder.Services.AddSingleton<UserSessionStore>();
builder.Services.AddSingleton<BotUpdateHandler>();
builder.Services.AddSingleton<UpdateIngestQueue>();
builder.Services.AddSingleton<ChannelTelegramUpdateIngress>();
builder.Services.AddSingleton<RabbitMqTelegramUpdateIngress>();
builder.Services.AddSingleton<ITelegramUpdateIngress>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<BotOptions>>().Value;
    return opts.RabbitMq.IsEnabled
        ? sp.GetRequiredService<RabbitMqTelegramUpdateIngress>()
        : sp.GetRequiredService<ChannelTelegramUpdateIngress>();
});
builder.Services.AddHostedService<TelegramUpdateDispatchWorker>();
builder.Services.AddHostedService<TelegramPollingWorker>();
builder.Services.AddHostedService<DraftCleanupWorker>();

builder.Services.Configure<HostOptions>(o =>
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await TelegramBotLifecycle.ConfigureAsync(scope.ServiceProvider);

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "bot" }));

var botOpts = app.Services.GetRequiredService<IOptions<BotOptions>>().Value;
if (BotDeliveryMode.IsWebhook(botOpts.DeliveryMode))
{
    var path = TelegramBotLifecycle.NormalizeWebhookPath(botOpts.WebhookPath);
    app.MapPost(path, async (HttpContext ctx, ITelegramUpdateIngress ingress, IOptions<BotOptions> opt) =>
    {
        var o = opt.Value;
        if (!string.IsNullOrEmpty(o.WebhookSecretToken))
        {
            if (!ctx.Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var sent) ||
                !string.Equals(sent.ToString(), o.WebhookSecretToken, StringComparison.Ordinal))
                return Results.Unauthorized();
        }

        Update? update;
        try
        {
            update = await JsonSerializer.DeserializeAsync<Update>(ctx.Request.Body, JsonBotAPI.Options, ctx.RequestAborted);
        }
        catch (JsonException)
        {
            return Results.BadRequest();
        }

        if (update is null)
            return Results.BadRequest();

        if (!await ingress.PublishAsync(update, UpdateIngressPublishMode.WebhookNoWait, ctx.RequestAborted))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        return Results.Ok();
    });
}

await app.RunAsync();
