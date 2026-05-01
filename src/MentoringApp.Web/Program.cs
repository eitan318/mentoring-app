using MentoringApp.ViewModel.DI;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Service;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.Web.Components;
using MentoringApp.Web.Services;
using Microsoft.Extensions.Configuration;


var builder = WebApplication.CreateBuilder(args);

// --- Add your shared services here ---
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
builder.Services.AddSingleton<ILanguageService, LanguageService>();
builder.Services.AddSingleton<IWindowService, MentoringApp.Web.Services.DummyWindowService>();
builder.Services.AddSingleton<IToastService, MentoringApp.Web.Services.DummyToastService>();
builder.Services.AddSingleton<IFileService, MentoringApp.Web.Services.DummyFileService>();

string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
builder.Services.AddViewModels(apiBaseUrl);

// Override the default WPF NavigationService with the URL-based Blazor variant.
// AddViewModels registers NavigationService as Singleton; replace it.
builder.Services.AddSingleton<NavigationParameterStore>();
var navServiceDescriptor = builder.Services.Single(d => d.ServiceType == typeof(INavigationService));
builder.Services.Remove(navServiceDescriptor);
builder.Services.AddScoped<INavigationService, WebNavigationService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
