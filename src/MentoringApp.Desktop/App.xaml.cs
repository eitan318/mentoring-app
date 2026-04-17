using MentoringApp.Data.Interfaces;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.DI;
using MentoringApp.ViewModel.ViewModel.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using MentoringApp.Data.DI;
using MentoringApp.ViewModel.DI;
using MentoringApp.Service.DI;
using MentoringApp.Service;
using MentoringApp.ViewModel.Store;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
namespace MentoringApp
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public App()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (connectionString == null)
            {
                throw new Exception("No connection string provided in config");
            }


            services.AddViewModels();
            services.AddDataRepositories(connectionString);
            services.AddServices(_configuration);
            services.AddView();

            _serviceProvider = services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Set to true during development to wipe and re-seed the database on every launch.
            // Must be false in production — data loss will occur if left enabled.
            bool recreateInitialDb = false;
            base.OnStartup(e);

            if (recreateInitialDb)
            {
                // Wipe any persisted session so the fresh DB isn't bypassed on the next launch.
                _serviceProvider.GetRequiredService<SessionService>().ClearSession();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbRepo>();
                    dbInitializer.Recreate();

                    var seeder = scope.ServiceProvider.GetRequiredService<DummyDataSeeder>();
                    await seeder.SeedAsync();
                }
            }

            // Show MainWindow first — this resolves MainWindowViewModel, which calls
            // UseContext() and registers the navigation context on the stack.
            // Any NavigateToAsync call before this point fires into an empty stack and is silently dropped.
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var mainVM = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = mainVM;
            mainWindow.Show();

            // Now the nav context exists — decide where to go.
            var navService = _serviceProvider.GetRequiredService<INavigationService>();

            if (!recreateInitialDb)
            {
                var sessionService = _serviceProvider.GetRequiredService<SessionService>();
                var userStore      = _serviceProvider.GetRequiredService<UserStore>();

                int? savedUserId = sessionService.LoadSession();
                if (savedUserId.HasValue)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
                    var userResult  = await userService.GetUserByIdAsync(savedUserId.Value);

                    if (userResult.Success && userResult.Data != null)
                    {
                        userStore.User = userResult.Data;
                        await navService.NavigateToAsync<AuthenticatedDashboardViewModel>();
                        return;
                    }

                    // Stored user no longer exists — wipe stale session
                    sessionService.ClearSession();
                }
            }

            await navService.NavigateToAsync<LoginViewModel>();
        }
    }
}


