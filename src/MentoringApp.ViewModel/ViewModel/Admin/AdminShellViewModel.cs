using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public partial class AdminShellViewModel : ObservableObject, INavigatable
{
    private readonly INavigationService _navigationService;
    private IDisposable? _innerNavContext;

    [ObservableProperty] private INavigatable? _currentAdminPage;
    [ObservableProperty] private string _activePage = "dashboard";

    public AdminShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public async Task OnNavigatedToAsync()
    {
        _innerNavContext = _navigationService.UseContext(vm =>
        {
            CurrentAdminPage = vm;
            ActivePage = vm switch
            {
                AdminDashboardViewModel => "dashboard",
                ManageUsersViewModel => "users",
                ManagePairsViewModel => "pairs",
                SystemSettingsViewModel => "settings",
                _ => ActivePage
            };
        });
        await _navigationService.NavigateToAsync<AdminDashboardViewModel>();
    }

    public Task OnNavigatedFromAsync()
    {
        _innerNavContext?.Dispose();
        _innerNavContext = null;
        return Task.CompletedTask;
    }

    [RelayCommand] private async Task GoToDashboard() => await _navigationService.NavigateToAsync<AdminDashboardViewModel>();
    [RelayCommand] private async Task GoToManageUsers() => await _navigationService.NavigateToAsync<ManageUsersViewModel>();
    [RelayCommand] private async Task GoToManagePairs() => await _navigationService.NavigateToAsync<ManagePairsViewModel>();
    [RelayCommand] private async Task GoToSystemSettings() => await _navigationService.NavigateToAsync<SystemSettingsViewModel>();
}
