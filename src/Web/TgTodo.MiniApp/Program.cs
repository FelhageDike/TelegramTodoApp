using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using TgTodo.MiniApp;
using TgTodo.MiniApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<TelegramInterop>();
builder.Services.AddScoped<ClientTimezoneState>();
builder.Services.AddScoped<ClientTimezoneDelegatingHandler>();
builder.Services.AddScoped<BffApiClient>(sp =>
{
    var handler = sp.GetRequiredService<ClientTimezoneDelegatingHandler>();
    handler.InnerHandler = new HttpClientHandler();
    var http = new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    return new BffApiClient(http, sp.GetRequiredService<TelegramInterop>());
});
builder.Services.AddScoped<GroupSelectionStorage>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<UserTimeService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
