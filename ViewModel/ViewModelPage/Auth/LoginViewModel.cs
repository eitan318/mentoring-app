using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Admin;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using MentoringApp.ViewModel.ViewModelPage.Student;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.Auth
{
    public partial class LoginViewModel : ObservableValidator, INavigatable
    {
        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;

        private static readonly bool _debugWithoutVerification = true;

        public LoginViewModel(UserStore userStore, INavigationService navigationService, AuthService authService)
        {
            _userStore = userStore;
            _authService = authService;
            _navigationService = navigationService;
        }

        [ObservableProperty] private bool _wasCodeSent;

        [ObservableProperty]
        [Required(ErrorMessage = "National ID is required")]
        private string _nationalId = "";
        
        [Required(ErrorMessage = "Code is required")]
        [ObservableProperty] private string _verificationCode = "";
        [ObservableProperty] private string _errorMessage = "";

        [RelayCommand]
        private async Task SendVerificationCode()
        {
            ValidateProperty(NationalId, nameof(NationalId));
            if (_debugWithoutVerification)
            {
                await Login();
                return;
            }

            ErrorMessage = "";
            var result = await _authService.SendVerificationCodeAsync(NationalId);
            
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to send code.";
                return;
            }
            
            WasCodeSent = true;
        }

        [RelayCommand]
        private async Task Login()
        {
            ValidateProperty(VerificationCode, nameof(VerificationCode));
            if (!_debugWithoutVerification)
            {
                var verificationResult = await _authService.VerificationCodeValid(VerificationCode);
                if (!verificationResult.Success)
                {
                    ErrorMessage = verificationResult.ErrorMessage ?? "Invalid code.";
                    return;
                }
            }

            var loginResult = await _authService.LoginAsync(NationalId);
            if (!loginResult.Success)
            {
                ErrorMessage = loginResult.ErrorMessage ?? "Login failed.";
                return;
            }

            var user = loginResult.Data;
            if (user != null)
            {
                _userStore.User = user;
                await NavigateBasedOnRole(user);
            }
        }

        private async Task NavigateBasedOnRole(User user)
        {
            await (user switch
            {
                Model.Admin => _navigationService.NavigateToAsync<AdminDashboardViewModel>(),
                Model.Supervisor => _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(user.Id),
                Model.Student => _navigationService.NavigateToAsync<StudentDashboardViewModel>(),
                _ => Task.CompletedTask
            });
        }
    }
}