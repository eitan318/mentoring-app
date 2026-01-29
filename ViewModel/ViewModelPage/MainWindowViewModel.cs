using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;

public partial class MainWindowViewModel : ObservableObject, INavigatable
{
    private readonly INavigationService _navigationService;
    private readonly IDisposable _navContext;

    [ObservableProperty] private INavigatable _activeSubPage;

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        _ = _navigationService.NavigateToAsync<LoginViewModel>();
    }

    public void OnWindowClosed()
    {
        _navContext?.Dispose(); 
    }

}