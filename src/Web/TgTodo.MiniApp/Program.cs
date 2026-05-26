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
builder.Services.AddScoped<BffApiClient>();
builder.Services.AddScoped<GroupSelectionStorage>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<UserTimeService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
