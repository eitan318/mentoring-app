using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using Microsoft.Extensions.DependencyInjection;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.Service;


namespace MentoringApp.ViewModel.DI
{
    public static class ViewModelDependencyInjection
    {
        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            // Stores (Singletons because they hold state)
            services.AddSingleton<UserStore>();

            // Services
            services.AddSingleton<INavigationService, NavigationService>();


            // ViewModels (Transient because you want a fresh one each time you navigate)
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<AuthenticatedDashboardViewModel>();
            services.AddTransient<RegistrationViewModel>();
            services.AddTransient<AdminDashboardViewModel>();
            services.AddTransient<StudentDashboardViewModel>();
            services.AddTransient<ManagePairsViewModel>();
            services.AddTransient<CreatePairViewModel>();
            services.AddTransient<SupervisorDashboardViewModel>();
            services.AddTransient<PairDetailsViewModel>();
            services.AddTransient<AddIssueViewModel>();
            services.AddTransient<AddReviewViewModel>();
            services.AddTransient<MyProfileViewModel>();
            services.AddTransient<OtherProfileViewModel>();
            services.AddTransient<ManageUsersViewModel>();
            services.AddTransient<BrowseMentorsViewModel>();
            services.AddTransient<SelectionGalleryViewModel>();
            services.AddTransient<MentorRequestsViewModel>();
            services.AddTransient<SystemSettingsViewModel>();

            return services;
        }
    }
}
