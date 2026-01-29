using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Admin;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class AuthenticatedDashboardViewModel : ObservableObject, INavigatable<Model.User>
    {
        private IDisposable _navContext;
        private readonly INavigationService _navigationService;

    [ObservableProperty] private INavigatable _;

        public AuthenticatedDashboardViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public async Task OnNavigatedToAsync(Model.User user)
        {
            _navContext = _navigationService.UseContext(vm => ActiveSubPage = vm);
            await (user switch
            {
                Model.Admin => _navigationService.NavigateToAsync<AdminDashboardViewModel>(),
                Model.Supervisor => _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(user.Id),
                Model.Student => _navigationService.NavigateToAsync<SupervisorDashboardViewModel>(),
                _ => Task.CompletedTask
            });

        }

        public async Task OnNavigatedFromAsync()
        {
            _navContext?.Dispose();
            await Task.CompletedTask;
        }
    }
}
