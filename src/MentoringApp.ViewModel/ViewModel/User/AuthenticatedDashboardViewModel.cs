using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.Auth;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User;

/// <summary>
/// Shell ViewModel that hosts the role-specific dashboard as a sub-page.
/// </summary>
public partial class AuthenticatedDashboardViewModel : ObservableObject, INavigatable
{
    private IDisposable? _navContext;
    private readonly INavigationService _navigationService;
    private readonly UserStore _userStore;
    private readonly ILanguageService _languageService;
    private readonly UserApiClient _userClient;
    private readonly SessionService _sessionService;
    private readonly AuthTokenStore _authTokenStore;

    public AuthenticatedDashboardViewModel(
        UserStore userStore,
        INavigationService navigationService,
        ILanguageService languageService,
        UserApiClient userClient,
        SessionService sessionService,
        AuthTokenStore authTokenStore)
    {
        _userStore = userStore;
        _navigationService = navigationService;
        _languageService = languageService;
        _userClient = userClient;
        _sessionService = sessionService;
        _authTokenStore = authTokenStore;

        _navigationService.CanGoBackChanged += OnCanGoBackChanged;
    }

    private void OnCanGoBackChanged() => OnPropertyChanged(nameof(IsBackVisible));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBackVisible))]
    [NotifyPropertyChangedFor(nameof(IsProfileButtonVisible))]
    private INavigatable? _activeSubPage;

    public bool IsProfileButtonVisible => ActiveSubPage is not MyProfileViewModel;
    public bool IsBackVisible => _navigationService.CanGoBack();

    [ObservableProperty] private UserModel? _currentUser;

    public System.Collections.ObjectModel.ObservableCollection<string> AvailableLanguages { get; } = ["en", "he"];
    [ObservableProperty] private string _selectedLanguage = "en";

    partial void OnSelectedLanguageChanged(string value)
    {
        _languageService.ApplyLanguage(value);
        if (CurrentUser != null)
            _ = _userClient.UpdateLanguageAsync(CurrentUser.Id, new UpdateLanguageRequest(value));
    }

    [RelayCommand]
    public async Task Back()
    {
        await _navigationService.GoBackAsync();
        // IsBackVisible is updated reactively via CanGoBackChanged
    }

    public async Task OnNavigatedToAsync()
    {
        CurrentUser = _userStore.User;

        if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Language))
        {
            // Bypass OnSelectedLanguageChanged to avoid saving to server on initial load
            SelectedLanguage = CurrentUser.Language;
            _languageService.ApplyLanguage(CurrentUser.Language);
        }

        _navContext = _navigationService.UseContext(vm => ActiveSubPage = vm);

        if (IsProfileIncomplete(CurrentUser))
        {
            await _navigationService.NavigateToAsync<MyProfileViewModel>();
            if (ActiveSubPage is MyProfileViewModel profileVm)
            {
                profileVm.IsReadOnly = false;
                profileVm.IsEditMode = true;
            }
            return;
        }

        if (CurrentUser == null) return;

        if (CurrentUser.IsAdmin)
            await _navigationService.NavigateToAsync<AdminDashboardViewModel>();
        else if (CurrentUser.IsSupervisor)
            await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(CurrentUser.Id);
        else if (CurrentUser.IsStudent)
            await _navigationService.NavigateToAsync<StudentDashboardViewModel>();
    }

    private static bool IsProfileIncomplete(UserModel? user)
    {
        if (user is not StudentModel student)
            return false;
        if (student.Grade == null || student.Grade.Id <= 0 || student.ClassNum <= 0)
            return true;
        if (student.IsMentor && (student.MentorProfile == null || student.MentorProfile.SubjectToTeach <= 0))
            return true;
        if (student.IsMentee && (student.MenteeProfile == null || student.MenteeProfile.SubjectToLearn <= 0))
            return true;

        return false;
    }

    [RelayCommand] private async Task NavigateProfile() => await _navigationService.NavigateToAsync<MyProfileViewModel>();

    [RelayCommand]
    private async Task Logout()
    {
        if (_navContext == null) return;

        _navigationService.CanGoBackChanged -= OnCanGoBackChanged;
        _sessionService.ClearSession();
        _authTokenStore.Clear();
        _userStore.User = null;

        // Cascade: let the child role dashboard pop its own nav context first.
        if (ActiveSubPage != null)
            await ActiveSubPage.OnNavigatedFromAsync();

        _navContext.Dispose();
        _navContext = null;

        await _navigationService.NavigateToAsync<LoginViewModel>();
    }
}
