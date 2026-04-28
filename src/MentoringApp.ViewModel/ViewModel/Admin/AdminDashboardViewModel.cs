using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public partial class AdminDashboardViewModel : ObservableObject, INavigatable
{
    private readonly INavigationService _navigationService;
    private IDisposable? _navContext;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOnDashboard))]
    [NotifyPropertyChangedFor(nameof(IsOnManagePairs))]
    [NotifyPropertyChangedFor(nameof(IsOnManageUsers))]
    [NotifyPropertyChangedFor(nameof(IsOnSystemSettings))]
    private INavigatable? _activeSubPage;

    public bool IsOnDashboard      => ActiveSubPage is AdminOverviewViewModel;
    public bool IsOnManagePairs    => ActiveSubPage is ManagePairsViewModel;
    public bool IsOnManageUsers    => ActiveSubPage is ManageUsersViewModel;
    public bool IsOnSystemSettings => ActiveSubPage is SystemSettingsViewModel;

    public AdminDashboardViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public async Task OnNavigatedToAsync()
    {
        _navContext = _navigationService.UseContext(vm => ActiveSubPage = vm);
        await _navigationService.NavigateToAsync<AdminOverviewViewModel>();
    }

    public Task OnNavigatedFromAsync()
    {
        _navContext?.Dispose();
        _navContext = null;
        return Task.CompletedTask;
    }

    [RelayCommand] private async Task GoToDashboard()      => await _navigationService.NavigateToAsync<AdminOverviewViewModel>();
    [RelayCommand] private async Task ManagePairs()        => await _navigationService.NavigateToAsync<ManagePairsViewModel>();
    [RelayCommand] private async Task ManageUsers()        => await _navigationService.NavigateToAsync<ManageUsersViewModel>();
    [RelayCommand] private async Task SystemSettings()     => await _navigationService.NavigateToAsync<SystemSettingsViewModel>();
}
