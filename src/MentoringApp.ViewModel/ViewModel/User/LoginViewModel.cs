using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.ViewModel.Auth;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class LoginViewModel : ObservableValidator, INavigatable
{
    private readonly AuthApiClient _authClient;
    private readonly UserApiClient _userClient;
    private readonly INavigationService _navigationService;
    private readonly UserStore _userStore;
    private readonly ILanguageService _languageService;
    private readonly SessionService _sessionService;
    private readonly AuthTokenStore _authTokenStore;

    // In debug mode verification code step is skipped.
    private static readonly bool _debugWithoutVerification = true;

    public LoginViewModel(
        AuthApiClient authClient,
        UserApiClient userClient,
        INavigationService navigationService,
        UserStore userStore,
        ILanguageService languageService,
        SessionService sessionService,
        AuthTokenStore authTokenStore)
    {
        _authClient = authClient;
        _userClient = userClient;
        _navigationService = navigationService;
        _userStore = userStore;
        _languageService = languageService;
        _sessionService = sessionService;
        _authTokenStore = authTokenStore;
    }

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
        ErrorMessage = "";
        try
        {
            var response = await _authClient.SendCodeAsync(new SendCodeRequest(NationalId));
            if (_debugWithoutVerification && response.DevCode is not null)
            {
                VerificationCode = response.DevCode;
                await Login();
                return;
            }
            WasCodeSent = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        ErrorMessage = "";
        try
        {
            var response = await _authClient.LoginAsync(new LoginRequest(NationalId, VerificationCode));

            // Decode claims from JWT (no library needed — just base64 the payload)
            var claims = DecodeJwtPayload(response.Token);
            claims.TryGetValue("sub", out var subVal);
            claims.TryGetValue("role", out var roleVal);
            claims.TryGetValue("language", out var langVal);

            if (!int.TryParse(subVal?.ToString(), out int userId))
            {
                ErrorMessage = "Failed to parse user ID from token.";
                return;
            }

            _authTokenStore.Token = response.Token;
            _authTokenStore.UserId = userId;
            _authTokenStore.Role = roleVal?.ToString();
            _authTokenStore.Language = langVal?.ToString();

            var user = await _userClient.GetByIdAsync(userId);
            if (user == null) { ErrorMessage = "User not found."; return; }

            if (_languageChangedOnLogin && user.Language != SelectedLanguage)
                await _userClient.UpdateLanguageAsync(user.Id, new UpdateLanguageRequest(SelectedLanguage));

            _sessionService.SaveSession(user.Id, response.Token);
            _userStore.User = user;

            await _navigationService.NavigateToAsync<AuthenticatedDashboardViewModel>();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private static Dictionary<string, object?> DecodeJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return new();
        var payload = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')
                               .Replace('-', '+').Replace('_', '/');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
    }
}
