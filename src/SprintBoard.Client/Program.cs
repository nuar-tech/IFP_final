using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SprintBoard.Client;
using SprintBoard.Client.Services;
using SprintBoard.Client.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = (builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7200").TrimEnd('/') + "/";

builder.Services.AddSingleton(new ApiSettings(apiBase));
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<Store>();
builder.Services.AddScoped<ThemeState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BoardHubClient>();
builder.Services.AddTransient<AuthHandler>();
builder.Services.AddHttpClient<ApiClient>(c => c.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<AuthHandler>();

await builder.Build().RunAsync();

