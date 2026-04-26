using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class MyProfileViewModel : ObservableValidator, INavigatable
{
    private readonly UserStore _userStore;
    private readonly UserApiClient _userClient;
    private readonly ReferenceApiClient _referenceClient;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private bool _isReadOnly = true;
    [ObservableProperty] private bool _isEditMode = false;
    [ObservableProperty] private string? _profilePicturePath;
    [ObservableProperty] private ObservableCollection<SubjectResponse> _subjects = [];
    [ObservableProperty] private ObservableCollection<GradeResponse> _grades = [];
    [ObservableProperty] private string _errorMessage = "";

    // Identity
    [ObservableProperty] private string _nationalId = "";

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email = "";

    [ObservableProperty]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3)]
    private string _userName = "";

    // Role Detection
    [ObservableProperty] private bool _hasMentorProfile;
    [ObservableProperty] private bool _hasMenteeProfile;
    [ObservableProperty] private bool _isSupervisor;
    [ObservableProperty] private bool _isAdmin;
    [ObservableProperty] private string _roleBadge = string.Empty;

    // Contact & Gender
    [ObservableProperty] private string? _phoneNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Gender))]
    private int _selectedGenderValue = (int)Gender.PreferNoAnswer;

    public Gender Gender => (Gender)SelectedGenderValue;

    // Student-specific
    [ObservableProperty] private GradeResponse? _selectedGrade;
    [ObservableProperty] private int _classNum;
    [ObservableProperty] private int _subjectToTeach = -1;
    [ObservableProperty] private int _subjectToLearn = -1;
    [ObservableProperty] private int _maxMentees = 1;
    [ObservableProperty] private int _selectedPreferredMentorGenderValue = (int)GenderPreference.NoPreference;
    [ObservableProperty] private int _selectedPreferredMenteeGenderValue = (int)GenderPreference.NoPreference;

    public static IReadOnlyList<GenderOption> GenderOptions { get; } = GenderHelper.GenderOptions;
    public static IReadOnlyList<GenderPreferenceOption> GenderPreferenceOptions { get; } = GenderHelper.GenderPreferenceOptions;

    public MyProfileViewModel(
        UserStore userStore,
        UserApiClient userClient,
        ReferenceApiClient referenceClient,
        INavigationService navigationService)
    {
        _userStore = userStore;
        _userClient = userClient;
        _referenceClient = referenceClient;
        _navigationService = navigationService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var grades = await _referenceClient.GetGradesAsync();
        var subjects = await _referenceClient.GetSubjectsAsync();

        Grades = new ObservableCollection<GradeResponse>(grades);
        Subjects = new ObservableCollection<SubjectResponse>(subjects);

        LoadUserData();
    }

    private void LoadUserData()
    {
        var user = _userStore.User;
        if (user == null) return;

        UserName = user.UserName;
        Email = user.Email;
        NationalId = user.NationalId;
        ProfilePicturePath = user.ProfilePicturePath;
        PhoneNumber = user.PhoneNumber;
        SelectedGenderValue = user.Gender;

        if (user.IsAdmin)
        {
            IsAdmin = true;
            IsSupervisor = false;
            RoleBadge = "Admin";
        }
        else if (user.IsStudent)
        {
            IsAdmin = false;
            IsSupervisor = false;
            SelectedGrade = Grades.FirstOrDefault(g => g.Id == user.GradeId);
            ClassNum = user.ClassNum ?? 0;
            SelectedPreferredMentorGenderValue = user.PreferredMentorGender ?? (int)GenderPreference.NoPreference;
            SelectedPreferredMenteeGenderValue = user.PreferredMenteeGender ?? (int)GenderPreference.NoPreference;
            HasMentorProfile = user.IsMentor;
            HasMenteeProfile = user.IsMentee;

            RoleBadge = (HasMentorProfile, HasMenteeProfile) switch
            {
                (true, true)  => "Student · Mentor & Mentee",
                (true, false) => "Student · Mentor",
                (false, true) => "Student · Mentee",
                _             => "Student"
            };

            if (HasMentorProfile) SubjectToTeach = user.MentorSubjectId ?? -1;
            if (HasMenteeProfile) SubjectToLearn = user.MenteeSubjectId ?? -1;
            MaxMentees = user.MaxMentees ?? 1;
        }
        else
        {
            IsAdmin = false;
            IsSupervisor = true;
            RoleBadge = "Supervisor";
        }
    }

    public async Task OnNavigatedToAsync()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsReadOnly = false;
        IsEditMode = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        var user = _userStore.User;
        if (user == null) return;

        try
        {
            await _userClient.UpdateBaseInfoAsync(user.Id, new UpdateBaseInfoRequest(UserName, Email, NationalId, PhoneNumber, SelectedGenderValue));

            if (user.IsStudent)
            {
                if (SelectedGrade != null)
                    await _userClient.UpdateGradeClassAsync(user.Id, new UpdateGradeClassRequest(SelectedGrade.Id, ClassNum));

                await _userClient.UpdateGenderPreferencesAsync(user.Id,
                    new UpdateGenderPreferencesRequest(SelectedPreferredMentorGenderValue, SelectedPreferredMenteeGenderValue));

                if (HasMentorProfile)
                    await _userClient.UpdateMentorProfileAsync(user.Id, new UpdateMentorProfileRequest(SubjectToTeach));
                if (HasMenteeProfile)
                    await _userClient.UpdateMenteeProfileAsync(user.Id, new UpdateMenteeProfileRequest(SubjectToLearn));
            }

            // Refresh UserStore with updated data
            var updated = await _userClient.GetByIdAsync(user.Id);
            if (updated != null) _userStore.User = updated;

            ErrorMessage = string.Empty;
            IsReadOnly = true;
            IsEditMode = false;

            if (!_navigationService.CanGoBack())
            {
                if (user.IsAdmin)
                    await _navigationService.NavigateToAsync<AdminDashboardViewModel>();
                else if (user.IsSupervisor)
                    await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(user.Id);
                else
                    await _navigationService.NavigateToAsync<StudentDashboardViewModel>();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UploadProfilePictureAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a Profile Picture",
            Filter = "Image Files|*.jpg;*.jpeg;*.png",
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;

        var user = _userStore.User;
        if (user == null) return;

        try
        {
            var newPath = await _userClient.UploadProfilePictureAsync(user.Id, dialog.FileName);
            if (newPath != null)
            {
                var updated = await _userClient.GetByIdAsync(user.Id);
                if (updated != null)
                {
                    _userStore.User = updated;
                    ProfilePicturePath = updated.ProfilePicturePath;
                }
            }
            else
            {
                ErrorMessage = "Failed to upload picture.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
