using MentoringApp.DI;
using MentoringApp.ViewModel.Auth;
using MentoringApp.ViewModel.DI;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace MentoringApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";

        var services = new ServiceCollection();
        services.AddViewModels(apiBaseUrl);
        services.AddView();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainVM     = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.DataContext = mainVM;
        mainWindow.Show();

        var navService = _serviceProvider.GetRequiredService<INavigationService>();

        var sessionService = _serviceProvider.GetRequiredService<SessionService>();
        var userStore      = _serviceProvider.GetRequiredService<UserStore>();
        var authTokenStore = _serviceProvider.GetRequiredService<AuthTokenStore>();

        var session = sessionService.LoadSession();
        if (session != null && TokenHasRoleClaim(session.Token))
        {
            authTokenStore.Token = session.Token;
            authTokenStore.UserId = session.UserId;
            // Eagerly fetch user to populate UserStore; if the API is unreachable, fall through to login.
            try
            {
                var userClient = _serviceProvider.GetRequiredService<MentoringApp.ApiClient.Clients.UserApiClient>();
                var user = await userClient.GetByIdAsync(session.UserId);
                if (user != null)
                {
                    userStore.User = user;
                    await navService.NavigateToAsync<AuthenticatedDashboardViewModel>();
                    return;
                }
            }
            catch
            {
                // Session token expired or API unreachable — clear and go to login
                sessionService.ClearSession();
                authTokenStore.Clear();
            }
        }
        else if (session != null)
        {
            // Token was minted with the old claim format — discard it.
            sessionService.ClearSession();
        }

        await navService.NavigateToAsync<LoginViewModel>();
    }

    private static bool TokenHasRoleClaim(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return false;
            var payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')
                                   .Replace('-', '+').Replace('_', '/');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var claims = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            return claims?.ContainsKey("role") == true;
        }
        catch { return false; }
    }
}
