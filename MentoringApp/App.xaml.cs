using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelPage;
using MentoringApp.ViewModel.ViewModelPage.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<MainWindow>();
            services.AddViewModels();
            services.AddDataRepositories(connectionString);
            services.AddServices(_configuration);

            _serviceProvider = services.BuildServiceProvider();
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            bool recreateInitialDb = true;
            base.OnStartup(e);

            if (recreateInitialDb)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbRepo>();
                    dbInitializer.Recreate();

                    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
                    userRepo.CreateUser(new Admin { Id = 0, Email = "eitanamir09@gmail.com", NationalId = "0", UserName = "Admin" });
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


