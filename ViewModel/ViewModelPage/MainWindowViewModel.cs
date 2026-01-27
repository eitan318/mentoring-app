using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Auth;


namespace MentoringApp.ViewModel.ViewModelPage
{
    public class MainWindowViewModel : ObservableObject, INavigatable
    {
        private readonly NavigationStore _navigationStore;
        private readonly INavigationService _navigationService;

        public MainWindowViewModel(
            NavigationStore navigationStore,
            INavigationService navigationService)
        {
            _navigationStore = navigationStore;
            _navigationService = navigationService;

            // Bind event handlers
            _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
            _navigationService.NavigateToAsync<LoginViewModel>();
        }

        public INavigatable CurrentViewModel => _navigationStore.CurrentViewModel;

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }

        public void OnWindowClosed()
        {
            // Unsubscribe event handlers to prevent memory leaks
            _navigationStore.CurrentViewModelChanged -= OnCurrentViewModelChanged;
        }
    }
}
