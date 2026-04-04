using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using System.ComponentModel.DataAnnotations;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class LoginViewModel : ObservableValidator, INavigatable
    {
        private readonly AuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;
        private readonly ILanguageService _languageService;
        private readonly SettingsService _settingsService;
        private readonly UserService _userService;

        private static readonly bool _debugWithoutVerification = true;

        public LoginViewModel(UserStore userStore, INavigationService navigationService, AuthService authService, ILanguageService languageService, SettingsService settingsService, UserService userService)
        {
            _userStore = userStore;
            _authService = authService;
            _navigationService = navigationService;
            _languageService = languageService;
            _settingsService = settingsService;
            _userService = userService;
        }

        // Language selection
        public ObservableCollection<string> AvailableLanguages { get; } = ["en", "he"];

        [ObservableProperty] private string _selectedLanguage = "en";

        private bool _languageChangedOnLogin;

        partial void OnSelectedLanguageChanged(string value)
        {
            _languageChangedOnLogin = true;
            _languageService.ApplyLanguage(value);
        }

        public Task OnNavigatedToAsync()
        {
            // Reset language choice flag and default to English every time the login screen is opened
            _languageChangedOnLogin = false;
            
            SelectedLanguage = "en";
            _languageService.ApplyLanguage("en");

            return Task.CompletedTask;
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
                // If user explicitly changed language on login screen, apply it to their profile.
                if (_languageChangedOnLogin && user.Language != SelectedLanguage)
                {
                    user.Language = SelectedLanguage;
                    _ = _userService.UpdateLanguageAsync(user.Id, SelectedLanguage);
                }
                
                _userStore.User = user;
                await _navigationService.NavigateToAsync<AuthenticatedDashboardViewModel>();
            }
        }
    }
}