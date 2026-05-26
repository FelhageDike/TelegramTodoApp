using TgTodo.Bot;
using TgTodo.Bot.Services;

var builder = Host.CreateApplicationBuilder(args);

var botToken = builder.Configuration["BOT_TOKEN"] ?? builder.Configuration["Bot:Token"];
builder.Services.Configure<BotOptions>(options =>
{
    builder.Configuration.GetSection(BotOptions.SectionName).Bind(options);
    if (!string.IsNullOrWhiteSpace(botToken))
        options.Token = botToken;
});
builder.Services.AddHttpClient<BffClient>();

builder.Services.AddSingleton<UserSessionStore>();
builder.Services.AddSingleton<InlineDraftStore>();
builder.Services.AddSingleton<BotUpdateHandler>();
builder.Services.AddHostedService<TelegramBotWorker>();

builder.Services.Configure<HostOptions>(o =>
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

var host = builder.Build();
await host.RunAsync();
