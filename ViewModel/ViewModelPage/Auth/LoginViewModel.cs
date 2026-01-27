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

            var result = await _authService.SendVerificationCodeAsync(NationalId);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }
            WasCodeSent = true;
        }

        private async void Login()
        {
            // Verification step
            if (!_debugWithoutVerification)
            {
                var verificationResult = await _authService.VerificationCodeValid(VerificationCode);
                if (!verificationResult.Success)
                {
                    ErrorMessage = verificationResult.ErrorMessage;
                    return;
                }
            }

            // Login step
            var loginResult = _authService.Login(NationalId);
            if (!loginResult.Success)
            {
                ErrorMessage = loginResult.ErrorMessage;
                return;
            }

            // Success - Navigation logic
            var user = loginResult.Data;
            _userStore.User = user;
    
            switch (user)
            {
                case Admin: await _navigationService.NavigateToAsync<AdminDashboardViewModel>(); break;
                case Supervisor: await _navigationService.NavigateToAsync<SupervisorDashboardViewModel>(); break;
                case Student: await _navigationService.NavigateToAsync<StudentHomeViewModel>(); break;
            }
        }

    }
}
