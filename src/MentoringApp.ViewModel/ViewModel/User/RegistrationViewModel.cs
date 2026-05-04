using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class RegistrationViewModel : ObservableValidator, INavigatable<bool>, ICloseable
{
    private readonly AuthApiClient _authClient;
    private readonly ReferenceApiClient _referenceClient;
    private readonly INavigationService _navigationService;

    public event Action? RequestClose;

    [ObservableProperty] private ObservableCollection<SubjectModel> _subjects = [];
    [ObservableProperty] private ObservableCollection<GradeModel> _grades = [];
    [ObservableProperty] private GradeModel? _selectedGrade;
    [ObservableProperty] private int _subjectToTeach = -1;
    [ObservableProperty] private int _subjectToLearn = -1;
    [ObservableProperty] private int _classNum = 1;

    [ObservableProperty] private bool _isMentor;
    [ObservableProperty] private bool _isMentee;
    [ObservableProperty] private bool _supervisorOrStudentIsSupervisor;
    [ObservableProperty] private string _errorMessage = "";
    [ObservableProperty] private string? _phoneNumber;
    [ObservableProperty] private int _selectedGenderValue = (int)Gender.PreferNoAnswer;
    [ObservableProperty] private int _selectedPreferredMentorGenderValue = (int)GenderPreference.NoPreference;
    [ObservableProperty] private int _selectedPreferredMenteeGenderValue = (int)GenderPreference.NoPreference;
    [ObservableProperty] private int _maxMentees = 1;

    public static IReadOnlyList<GenderOption> GenderOptions { get; } = GenderHelper.GenderOptions;
    public static IReadOnlyList<GenderPreferenceOption> GenderPreferenceOptions { get; } = GenderHelper.GenderPreferenceOptions;

    [ObservableProperty] [Required] private string _nationalId = "";

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email = "";

    [ObservableProperty]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3)]
    private string _userName = "";

    public RegistrationViewModel(
        AuthApiClient authClient,
        ReferenceApiClient referenceClient,
        INavigationService navigationService)
    {
        _authClient = authClient;
        _referenceClient = referenceClient;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        ErrorMessage = "";

        var request = new RegisterRequest(
            UserName: UserName,
            Email: Email,
            NationalId: NationalId,
            PhoneNumber: PhoneNumber,
            Gender: SelectedGenderValue,
            Role: SupervisorOrStudentIsSupervisor ? "Supervisor" : "Student",
            GradeId: SelectedGrade?.Id,
            ClassNum: ClassNum,
            PreferredMentorGender: SelectedPreferredMentorGenderValue,
            PreferredMenteeGender: SelectedPreferredMenteeGenderValue,
            MentorSubjectId: IsMentor ? SubjectToTeach : null,
            MaxMentees: IsMentor ? MaxMentees : null,
            MenteeSubjectId: IsMentee ? SubjectToLearn : null);

        try
        {
            await _authClient.RegisterAsync(request);
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task OnNavigatedToAsync(bool supervisorOrStudentIsSupervisor)
    {
        SupervisorOrStudentIsSupervisor = supervisorOrStudentIsSupervisor;

        var subjects = await _referenceClient.GetSubjectsAsync();
        Subjects = new ObservableCollection<SubjectModel>(subjects);

        var grades = await _referenceClient.GetGradesAsync();
        Grades = new ObservableCollection<GradeModel>(grades);
    }
}
