using MentoringApp.ViewModel.Store;

using MentoringApp.ViewModel.ViewModelHelper;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Service;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelPage;

namespace MentoringApp.ViewModel.ViewModelPage
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _loginService;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;

        public LoginViewModel(UserStore userStore, INavigationService navigationService, AuthService loginService)
        {  
            _userStore = userStore;
            _loginService = loginService;
            _navigationService = navigationService;
            LoginCommand = new RelayCommand(Login);
        }

        private string _nationalId = "";
        public string NationalId { get => _nationalId; set => SetProperty(ref _nationalId, value); }
        private string _errorMessage = "";
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        public ICommand LoginCommand { get; }

        private async void Login() // Use async if your navigation is Task-based
        {
            var loggedInUser = _loginService.Login(NationalId);

            if (loggedInUser is User user)
            {
                _userStore.User = user;
                ErrorMessage = "";

                if (user is Admin)
                {
                    await _navigationService.NavigateToAsync<AdminDashboardViewModel>();
                }
                if (user is Supervisor)
                {
                    await _navigationService.NavigateToAsync<SupervisorDashboardViewModel>();
                }
                else if (user is Student student)
                {
                    await _navigationService.NavigateToAsync<StudentHomeViewModel>();
                }

                return;
            }

            ErrorMessage = $"No user with national ID: {NationalId}";
        }

    }
}
