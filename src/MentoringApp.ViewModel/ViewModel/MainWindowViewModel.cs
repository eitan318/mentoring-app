using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.User;

/// <summary>Root shell view model bound to the main window: hosts the currently displayed view model and owns the top-level navigation context.</summary>
public partial class MainWindowViewModel : ObservableObject, INavigatable
{
    private readonly INavigationService _navigationService;
    private readonly IDisposable _navContext;

    [ObservableProperty] private INavigatable _currentViewModel;


    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navContext = navigationService.UseContext(vm => CurrentViewModel = vm);
        // Initial navigation is handled by App.xaml.cs after the window is shown,
        // so the nav context already exists when it fires.
    }

    public void OnWindowClosed()
    {
        _navContext?.Dispose(); 
    }

}