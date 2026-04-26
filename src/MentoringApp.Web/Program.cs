using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MentoringApp.ApiClient.Extensions;
using MentoringApp.Web;
using MentoringApp.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5035";

builder.Services.AddSingleton<AuthStateService>();
builder.Services.AddTransient<JwtBearerHandler>();
builder.Services.AddApiClientsWithAuth<JwtBearerHandler>(apiBaseUrl);

await builder.Build().RunAsync();
