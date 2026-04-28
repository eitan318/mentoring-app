using MentoringApp.ApiClient.Extensions;
using MentoringApp.ViewModel.Auth;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModel.User;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.ViewModel.DI;

public static class ViewModelDependencyInjection
{
    public static IServiceCollection AddViewModels(this IServiceCollection services, string apiBaseUrl)
    {
        // Auth token store (singleton — holds the JWT across requests)
        services.AddSingleton<AuthTokenStore>();

        // Session persistence
        services.AddSingleton<SessionService>();

        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Stores
        services.AddSingleton<UserStore>();

        // HTTP clients with bearer-token handler
        services.AddApiClientsWithAuth<BearerTokenHandler>(apiBaseUrl);

        // ViewModels (Transient — fresh instance per navigation)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AuthenticatedDashboardViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<AdminDashboardViewModel>();
        services.AddTransient<AdminOverviewViewModel>();
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
