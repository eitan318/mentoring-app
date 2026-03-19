using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Admin;
using MentoringApp.ViewModel.ViewModelPage.Student;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class AuthenticatedDashboardViewModel : ObservableObject, INavigatable
    {
        private IDisposable _navContext;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;

        public AuthenticatedDashboardViewModel(UserStore userStore, INavigationService navigationService)
        {
            _userStore = userStore;
            _navigationService = navigationService;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBackVisible))]
        [NotifyPropertyChangedFor(nameof(IsProfileButtonVisible))]
        private INavigatable? _activeSubPage;

        // Returns true only if we are NOT currently on the Profile page
        public bool IsProfileButtonVisible => ActiveSubPage is not MyProfileViewModel;
        public bool IsBackVisible => _navigationService.CanGoBack();

        [ObservableProperty] private UserModel? _currentUser;

        [RelayCommand]
        public async Task Back()
        {
            await _navigationService.GoBackAsync();
            OnPropertyChanged(nameof(IsBackVisible));
        }

        public async Task OnNavigatedToAsync()
        {
            CurrentUser = _userStore.User;
            _navContext = _navigationService.UseContext(vm => ActiveSubPage = vm);
            await (CurrentUser switch
            {
                AdminModel => _navigationService.NavigateToAsync<AdminDashboardViewModel>(),
                SupervisorModel => _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(CurrentUser.Id),
                StudentModel => _navigationService.NavigateToAsync<StudentDashboardViewModel>(),
                _ => Task.CompletedTask
            });

        }

        [RelayCommand] private void NavigateProfile()
        {
            _navigationService.NavigateToAsync<MyProfileViewModel>();
        }
        [RelayCommand] private void Logout()
        {
            _navContext?.Dispose();
            _navigationService.NavigateToAsync<LoginViewModel>();
        }
    }
}
