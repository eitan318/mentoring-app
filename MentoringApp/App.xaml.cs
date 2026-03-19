using MentoringApp.Data.Interfaces;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.DI;
using MentoringApp.ViewModel.ViewModelPage.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using MentoringApp.Data.DI;
using MentoringApp.ViewModel.DI;
using MentoringApp.Service.DI;

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
            bool recreateInitialDb = true;
            base.OnStartup(e);

            if (recreateInitialDb)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Drop & recreate all tables
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbRepo>();
                    dbInitializer.Recreate();

                    // Seed comprehensive dummy data for all app flows
                    var seeder = scope.ServiceProvider.GetRequiredService<DummyDataSeeder>();
                    await seeder.SeedAsync();
                }
            }

            // Initial Navigation
            var navService = _serviceProvider.GetRequiredService<INavigationService>();
            navService.NavigateToAsync<LoginViewModel>();

            // Show MainWindow manually
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var mainVM = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = mainVM;

            mainWindow.Show();
        }
    }
}


