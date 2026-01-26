using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
namespace MentoringApp
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        protected override void OnStartup(StartupEventArgs e)
        {
            bool recreateInitialDb = true;

            base.OnStartup(e);
            
            var dbPath = Path.Combine(
                AppContext.BaseDirectory,
                //MentoringApp\    -  MentoringApp\bin\Debug\net9.0-windows
                "..", "..", "..", "..",
                //MentoringApp\    -  Data\Resources\Database
                "Data", "Resources", "Database", "mentoring.db"
            );

            // DI Setup
            var services = new ServiceCollection();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<MainWindow>();
            services.AddViewModels();

            services.AddDataRepositories($"Data Source={dbPath}");
            services.AddServices();
            _serviceProvider = services.BuildServiceProvider();

            if (recreateInitialDb)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IRecreateDb>();
                    dbInitializer.Recreate();

                    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
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


