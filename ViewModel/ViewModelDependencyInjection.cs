using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage;
using MentoringApp.ViewModel.ViewModelPage.Admin;
using MentoringApp.ViewModel.ViewModelPage.Auth;
using MentoringApp.ViewModel.ViewModelPage.Dashboard;
using Microsoft.Extensions.DependencyInjection;


namespace MentoringApp.ViewModel
{
    public static class ViewModelDependencyInjection
    {
        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            // Stores (Singletons because they hold state)
            services.AddSingleton<NavigationStore>();
            services.AddSingleton<UserStore>();

            // Services
            services.AddSingleton<INavigationService, NavigationService>();


            // ViewModels (Transient because you want a fresh one each time you navigate)
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<RegistrationViewModel>();
            services.AddTransient<AdminDashboardViewModel>();
            services.AddTransient<SupervisorDashboardViewModel>();
            services.AddTransient<StudentHomeViewModel>();
            services.AddTransient<ManagePairsViewModel>();
            services.AddTransient<CreatePairViewModel>();
            services.AddTransient<SupervisorViewModel>();
            services.AddTransient<AllSupervisorsViewModel>();

            return services;
        }
    }
}
