using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Extensions;
using MentoringApp.Components.Auth;
using MentoringApp.Components.Services;
using MentoringApp.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<MentoringApp.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

// AuthState is a plain singleton — no JS interop, safe for IHttpClientFactory handler scope.
// WasmAuthService (Scoped) writes to it; BearerTokenHandler (Scoped) reads from it.
builder.Services.AddSingleton<AuthState>();
builder.Services.AddScoped<WasmAuthService>();
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<WasmAuthService>());
builder.Services.AddScoped<BearerTokenHandler>();
builder.Services.AddSingleton<LayoutStateService>();

// Typed API clients with bearer token handler
builder.Services.AddHttpClient<AuthApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddHttpClient<UserApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<PairApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<MatchingApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<IssueApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<ReviewApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<ReferenceApiClient>(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

await builder.Build().RunAsync();
