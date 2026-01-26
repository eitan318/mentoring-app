using MentoringApp.ViewModel.Store;

using MentoringApp.ViewModel.ViewModelHelper;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Service;
using MentoringApp.Model;

namespace MentoringApp.ViewModel.ViewModelPage
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;

        private static readonly bool _debugWithoutVerification = true;

        public LoginViewModel(UserStore userStore, INavigationService navigationService, AuthService loginService)
        {  
            _userStore = userStore;
            _authService = loginService;
            _navigationService = navigationService;
            LoginCommand = new RelayCommand(Login);
            SendVerificationCodeCommand = new RelayCommand(SendVerificationCode);
        }
        private bool _wasCodeSent = false;
        public bool WasCodeSent { get => _wasCodeSent; set => SetProperty(ref _wasCodeSent, value); }

        private string _nationalId = "";
        public string NationalId { get => _nationalId; set => SetProperty(ref _nationalId, value); }

        private string _verificationCode = "";
        public string VerificationCode { get => _verificationCode; set => SetProperty(ref _verificationCode, value); }
        private string _errorMessage = "";
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        public ICommand LoginCommand { get; }
        public ICommand SendVerificationCodeCommand { get; }

        private async void SendVerificationCode()
        {
            if (_debugWithoutVerification)
            {
                Login();
            }

            bool result = await _authService.SendVerificationCodeAsync(NationalId);
            if (!result)
            {
                ErrorMessage = $"Couldnt send verification code: {NationalId}";
                return;
            }
            WasCodeSent = true;
        }

        private async void Login()
        {
            if (!_debugWithoutVerification && !await _authService.VerificationCodeValid(VerificationCode))
            {
                ErrorMessage = "Verification code invalid";
                return;
            }
            var loggedInUser = _authService.Login(NationalId);

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
