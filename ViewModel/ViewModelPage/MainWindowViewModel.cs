using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;

public partial class MainWindowViewModel : ObservableObject, INavigatable
{
    private readonly INavigationService _navigationService;
    private readonly IDisposable _navContext;

    [ObservableProperty] private INavigatable _currentViewModel;


    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        navigationService.UseContext(vm => CurrentViewModel = vm);
        _ = _navigationService.NavigateToAsync<LoginViewModel>();
    }

    public void OnWindowClosed()
    {
        _navContext?.Dispose(); 
    }

}