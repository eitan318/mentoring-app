using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.User 
{
    public partial class RegistrationViewModel : ObservableValidator, INavigatable<bool>, ICloseable
    {
        private readonly AuthService _authService;
        private readonly SubjectService _subjectService;
        private readonly GradeService _gradeService;
        public event Action? RequestClose;

        private readonly INavigationService _navigationService;

        // Inside RegistrationViewModel
        [ObservableProperty] private ObservableCollection<Subject> _subjects = [];
        [ObservableProperty] private ObservableCollection<Grade> _grades = [];

        [ObservableProperty]
        private Grade? _selectedGrade = null;

        [ObservableProperty]
        private int _subjectToTeach = -1;

        [ObservableProperty]
        private int _subjectToLearn = -1;


        public RegistrationViewModel(AuthService authService, INavigationService navigationService, SubjectService subjectService, GradeService gradeService)
        {
            _authService = authService;
            _navigationService = navigationService; // New
            _subjectService = subjectService;
            _gradeService = gradeService;
            Subjects = new ObservableCollection<Model.Subject>();
        }

        [ObservableProperty] private bool _isMentor;
        [ObservableProperty] private bool _isMentee;
        [ObservableProperty] private bool _supervisorOrStudentIsSupervisor;
        [ObservableProperty] private string _errorMessage = "";
        [ObservableProperty] [Required] private string _nationalId = "";

        [ObservableProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        private string _email = "";

        [ObservableProperty]
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3)]
        private string _userName = "";



        [RelayCommand]
        private async Task RegisterAsync()
        {
            ValidateAllProperties();
            if (HasErrors) return;

            ErrorMessage = "";

            var user = CreateUserFromState();
            var result = await _authService.Register(user);

            if (result.Success)
            {
                await _navigationService.GoBackAsync();
            }
            else
            {
                HandleServerResult(result);
            }
        }
        partial void OnIsMentorChanged(bool value) => ValidateProperty(SubjectToTeach, nameof(SubjectToTeach));

        private UserModel CreateUserFromState()
        {
            if (SupervisorOrStudentIsSupervisor)
            {
                return new SupervisorModel { UserName = UserName, Email = Email, NationalId = NationalId };
            }

            var student = new StudentModel { UserName = UserName, Email = Email, NationalId = NationalId, Grade = SelectedGrade };
            if (IsMentee) student.MenteeProfile = new MenteeProfile { SubjectToLearn = SubjectToLearn };
            if (IsMentor) student.MentorProfile = new MentorProfile { SubjectToTeach = SubjectToTeach };

            return student;
        }

        private void HandleServerResult(Result<UserModel> result)
        {
            if (result.ValidationErrors != null && result.ValidationErrors.Any())
            {
                ErrorMessage = string.Join(Environment.NewLine,
                    result.ValidationErrors.Select(x => $"• {x.Value}"));
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }

        public async Task OnNavigatedToAsync(bool supervisorOrStudentIsSupervisor)
        {
            this.SupervisorOrStudentIsSupervisor = supervisorOrStudentIsSupervisor;

            var subjects = await _subjectService.GetAllSubjectsAsync();
            Subjects = new ObservableCollection<Subject>(subjects.Data);

            var grades = await _gradeService.GetAllGradesAsync();
            Grades = new ObservableCollection<Grade>(grades.Data);
        }
    }
}