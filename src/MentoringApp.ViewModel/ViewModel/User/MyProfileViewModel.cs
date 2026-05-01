using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles; // וודא שה-Namespace הזה קיים
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.Student;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.IService;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class MyProfileViewModel : ObservableValidator, INavigatable
{
    private readonly UserStore _userStore;
    private readonly UserApiClient _userClient;
    private readonly ReferenceApiClient _referenceClient;
    private readonly INavigationService _navigationService;
    private readonly IFileService _fileService;

    [ObservableProperty] private bool _isReadOnly = true;
    [ObservableProperty] private bool _isEditMode = false;
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty] private ObservableCollection<SubjectModel> _subjects = [];
    [ObservableProperty] private ObservableCollection<GradeModel> _grades = [];

    // האובייקט המרכזי - ה-Data Binding ב-XAML יתבצע ישירות מולו
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UserName))]
    [NotifyPropertyChangedFor(nameof(Email))]
    [NotifyPropertyChangedFor(nameof(NationalId))]
    [NotifyPropertyChangedFor(nameof(PhoneNumber))]
    [NotifyPropertyChangedFor(nameof(ProfilePicturePath))]
    [NotifyPropertyChangedFor(nameof(Gender))]
    [NotifyPropertyChangedFor(nameof(SelectedGenderValue))]
    [NotifyPropertyChangedFor(nameof(IsSupervisor))]
    [NotifyPropertyChangedFor(nameof(SelectedGrade))]
    [NotifyPropertyChangedFor(nameof(ClassNum))]
    [NotifyPropertyChangedFor(nameof(HasMentorProfile))]
    [NotifyPropertyChangedFor(nameof(HasMenteeProfile))]
    [NotifyPropertyChangedFor(nameof(SubjectToTeach))]
    [NotifyPropertyChangedFor(nameof(MaxMentees))]
    [NotifyPropertyChangedFor(nameof(SubjectToLearn))]
    [NotifyPropertyChangedFor(nameof(SelectedPreferredMentorGenderValue))]
    [NotifyPropertyChangedFor(nameof(SelectedPreferredMenteeGenderValue))]
    private UserModel? _currentUser;

    [ObservableProperty] private string _roleBadge = string.Empty;

    // ── Proxy properties bound by MyProfileView.xaml ──────────────────────────
    public string UserName
    {
        get => CurrentUser?.UserName ?? "";
        set { if (CurrentUser != null) CurrentUser.UserName = value; }
    }
    public string Email
    {
        get => CurrentUser?.Email ?? "";
        set { if (CurrentUser != null) CurrentUser.Email = value; }
    }
    public string NationalId => CurrentUser?.NationalId ?? "";
    public string? PhoneNumber
    {
        get => CurrentUser?.PhoneNumber;
        set { if (CurrentUser != null) CurrentUser.PhoneNumber = value; }
    }
    public string ProfilePicturePath => CurrentUser?.ProfilePicturePath ?? "";
    public Gender Gender => CurrentUser?.Gender ?? Gender.PreferNoAnswer;

    public int SelectedGenderValue
    {
        get => (int)(CurrentUser?.Gender ?? Gender.PreferNoAnswer);
        set { if (CurrentUser != null) CurrentUser.Gender = (Gender)value; }
    }

    public bool IsSupervisor => CurrentUser is SupervisorModel;

    private StudentModel? AsStudent => CurrentUser as StudentModel;
    public bool HasMentorProfile => AsStudent?.IsMentor == true;
    public bool HasMenteeProfile => AsStudent?.IsMentee == true;

    public GradeModel? SelectedGrade
    {
        get => AsStudent?.Grade;
        set { if (AsStudent != null && value != null) AsStudent.Grade = value; }
    }

    public int ClassNum
    {
        get => AsStudent?.ClassNum ?? 0;
        set { if (AsStudent != null) AsStudent.ClassNum = value; }
    }

    public int SubjectToTeach
    {
        get => AsStudent?.MentorProfile?.SubjectToTeach ?? 0;
        set { if (AsStudent?.MentorProfile != null) AsStudent.MentorProfile.SubjectToTeach = value; }
    }

    public int MaxMentees
    {
        get => AsStudent?.MentorProfile?.MaxMentees ?? 1;
        set { if (AsStudent?.MentorProfile != null) AsStudent.MentorProfile.MaxMentees = value; }
    }

    public int SubjectToLearn
    {
        get => AsStudent?.MenteeProfile?.SubjectToLearn ?? 0;
        set { if (AsStudent?.MenteeProfile != null) AsStudent.MenteeProfile.SubjectToLearn = value; }
    }

    public int SelectedPreferredMentorGenderValue
    {
        get => (int)(AsStudent?.PreferredMentorGender ?? GenderPreference.NoPreference);
        set { if (AsStudent != null) AsStudent.PreferredMentorGender = (GenderPreference)value; }
    }

    public int SelectedPreferredMenteeGenderValue
    {
        get => (int)(AsStudent?.PreferredMenteeGender ?? GenderPreference.NoPreference);
        set { if (AsStudent != null) AsStudent.PreferredMenteeGender = (GenderPreference)value; }
    }

    public static IReadOnlyList<GenderOption> GenderOptions { get; } = GenderHelper.GenderOptions;
    public static IReadOnlyList<GenderPreferenceOption> GenderPreferenceOptions { get; } = GenderHelper.GenderPreferenceOptions;

    public MyProfileViewModel(
        UserStore userStore,
        UserApiClient userClient,
        ReferenceApiClient referenceClient,
        INavigationService navigationService,
        IFileService fileService)
    {
        _userStore = userStore;
        _userClient = userClient;
        _referenceClient = referenceClient;
        _navigationService = navigationService;
        _fileService = fileService;
    }

    public async Task OnNavigatedToAsync()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var grades = await _referenceClient.GetGradesAsync();
        var subjects = await _referenceClient.GetSubjectsAsync();

        Grades = new ObservableCollection<GradeModel>(grades);
        Subjects = new ObservableCollection<SubjectModel>(subjects);

        LoadUserData();
    }

    private void LoadUserData()
    {
        if (_userStore.User == null) return;

        // אנחנו עובדים על העותק שנמצא ב-Store
        CurrentUser = _userStore.User;

        // קביעת ה-Badge באמצעות Pattern Matching על סוג האובייקט
        RoleBadge = CurrentUser switch
        {
            AdminModel => "Admin",
            SupervisorModel => "Supervisor",
            StudentModel s => (s.IsMentor, s.IsMentee) switch
            {
                (true, true) => "Student · Mentor & Mentee",
                (true, false) => "Student · Mentor",
                (false, true) => "Student · Mentee",
                _ => "Student"
            },
            _ => "User"
        };
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
        if (CurrentUser == null) return;

        // וולידציה בסיסית (במידה והוספת Attributes למודל ב-Shared)
        ValidateAllProperties();
        if (HasErrors) return;

        try
        {
            // 1. עדכון פרטי בסיס
            await _userClient.UpdateBaseInfoAsync(CurrentUser.Id,
                new UpdateBaseInfoRequest(
                    CurrentUser.UserName,
                    CurrentUser.Email,
                    CurrentUser.NationalId,
                    CurrentUser.PhoneNumber,
                    (int)CurrentUser.Gender));

            // 2. לוגיקה ספציפית לסטודנט
            if (CurrentUser is StudentModel student)
            {
                // עדכון כיתה
                if (student.Grade != null)
                {
                    await _userClient.UpdateGradeClassAsync(student.Id,
                        new UpdateGradeClassRequest(student.Grade.Id, student.ClassNum));
                }

                // עדכון העדפות מגדר
                await _userClient.UpdateGenderPreferencesAsync(student.Id,
                    new UpdateGenderPreferencesRequest(
                        (int)student.PreferredMentorGender,
                        (int)student.PreferredMenteeGender));

                // עדכון פרופילי מנטור/מנטי
                if (student.IsMentor && student.MentorProfile != null)
                {
                    await _userClient.UpdateMentorProfileAsync(student.Id,
                        new UpdateMentorProfileRequest(student.MentorProfile.SubjectToTeach));
                }

                if (student.IsMentee && student.MenteeProfile != null)
                {
                    await _userClient.UpdateMenteeProfileAsync(student.Id,
                        new UpdateMenteeProfileRequest(student.MenteeProfile.SubjectToLearn));
                }
            }

            // 3. רענון הנתונים מהשרת לאחר השמירה
            var updated = await _userClient.GetByIdAsync(CurrentUser.Id);
            if (updated != null)
            {
                _userStore.User = updated;
                CurrentUser = updated;
            }

            ErrorMessage = string.Empty;
            IsReadOnly = true;
            IsEditMode = false;

            // ניווט חזרה לדשבורד המתאים במידה וצריך
            await NavigateBackToDashboard();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Save failed: " + ex.Message;
        }
    }

    private async Task NavigateBackToDashboard()
    {
        if (_navigationService.CanGoBack()) return;

        if (CurrentUser is AdminModel)
            await _navigationService.NavigateToAsync<AdminDashboardViewModel>();
        else if (CurrentUser is SupervisorModel)
            await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(CurrentUser.Id);
        else
            await _navigationService.NavigateToAsync<StudentDashboardViewModel>();
    }

    [RelayCommand]
    private async Task UploadProfilePictureAsync()
    {
        var filePath = _fileService.OpenFile("Image Files|*.jpg;*.jpeg;*.png");

        if (string.IsNullOrEmpty(filePath) || CurrentUser == null) return;

        try
        {
            var newPath = await _userClient.UploadProfilePictureAsync(CurrentUser.Id, filePath);
            if (newPath != null)
            {
                var updated = await _userClient.GetByIdAsync(CurrentUser.Id);
                if (updated != null)
                {
                    _userStore.User = updated;
                    CurrentUser = updated;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Upload failed: " + ex.Message;
        }
    }
}