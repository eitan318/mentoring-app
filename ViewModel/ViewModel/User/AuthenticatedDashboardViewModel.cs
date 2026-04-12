using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace MentoringApp.ViewModel.ViewModel.User
{
    /// <summary>
    /// Shell ViewModel that hosts the role-specific dashboard as a sub-page.
    /// On login it opens a nested navigation context (<see cref="INavigationService.UseContext"/>)
    /// so sub-page navigation doesn't affect the outer (login) history stack.
    /// If the student's profile is incomplete, it immediately redirects to
    /// <see cref="MyProfileViewModel"/> in edit mode before showing the dashboard.
    /// </summary>
    public partial class AuthenticatedDashboardViewModel : ObservableObject, INavigatable
    {
        // Disposing this handle pops the nested navigation context (sub-page area) on logout.
        private IDisposable _navContext;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;
        private readonly ILanguageService _languageService;
        private readonly UserService _userService;
        private readonly SessionService _sessionService;

        public AuthenticatedDashboardViewModel(UserStore userStore, INavigationService navigationService, ILanguageService languageService, UserService userService, SessionService sessionService)
        {
            _userStore = userStore;
            _navigationService = navigationService;
            _languageService = languageService;
            _userService = userService;
            _sessionService = sessionService;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBackVisible))]
        [NotifyPropertyChangedFor(nameof(IsProfileButtonVisible))]
        private INavigatable? _activeSubPage;

        // Returns true only if we are NOT currently on the Profile page
        public bool IsProfileButtonVisible => ActiveSubPage is not MyProfileViewModel;
        public bool IsBackVisible => _navigationService.CanGoBack();

        [ObservableProperty] private UserModel? _currentUser;

        // Language selection — available from any page via the header ComboBox
        public System.Collections.ObjectModel.ObservableCollection<string> AvailableLanguages { get; } = ["en", "he"];

        [ObservableProperty] private string _selectedLanguage = "en";

        partial void OnSelectedLanguageChanged(string value)
        {
            _languageService.ApplyLanguage(value);
            if (CurrentUser != null)
            {
                CurrentUser.Language = value;
                _ = _userService.UpdateLanguageAsync(CurrentUser.Id, value);
            }
        }

        [RelayCommand]
        public async Task Back()
        {
            await _navigationService.GoBackAsync();
            OnPropertyChanged(nameof(IsBackVisible));
        }

        public async Task OnNavigatedToAsync()
        {
            CurrentUser = _userStore.User;

            // Apply user's saved language preference immediately on login
            if (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Language))
            {
                _selectedLanguage = CurrentUser.Language; // set backing field to avoid re-triggering save
                OnPropertyChanged(nameof(SelectedLanguage));
                _languageService.ApplyLanguage(CurrentUser.Language);
            }

            _navContext = _navigationService.UseContext(vm => ActiveSubPage = vm);

            if (IsProfileIncomplete(CurrentUser))
            {
                await _navigationService.NavigateToAsync<MyProfileViewModel>();
                // Explicitly toggle edit mode so they know they need to fill it out
                if (ActiveSubPage is MyProfileViewModel profileVm)
                {
                    profileVm.IsReadOnly = false;
                    profileVm.IsEditMode = true;
                }
                return;
            }

            await (CurrentUser switch
            {
                AdminModel => _navigationService.NavigateToAsync<AdminDashboardViewModel>(),
                SupervisorModel => _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(CurrentUser.Id),
                StudentModel => _navigationService.NavigateToAsync<StudentDashboardViewModel>(),
                _ => Task.CompletedTask
            });
        }

        /// <summary>
        /// Returns true if a student is missing any required profile fields.
        /// Admin and Supervisor accounts are always considered complete.
        /// Required fields: Grade (non-zero id), ClassNum > 0, and subject IDs for any active role.
        /// </summary>
        private bool IsProfileIncomplete(UserModel? user)
        {
            if (user is StudentModel student)
            {
                bool incomplete = student.Grade == null || student.Grade.Id == 0 || student.ClassNum <= 0;
                if (student.IsMentor && student.MentorProfile?.SubjectToTeach <= 0) incomplete = true;
                if (student.IsMentee && student.MenteeProfile?.SubjectToLearn <= 0) incomplete = true;
                return incomplete;
            }
            return false;
        }

        [RelayCommand] private void NavigateProfile()
        {
            _navigationService.NavigateToAsync<MyProfileViewModel>();
        }
        [RelayCommand] private void Logout()
        {
            _sessionService.ClearSession();
            _navContext?.Dispose();
            _navigationService.NavigateToAsync<LoginViewModel>();
        }
    }
}
