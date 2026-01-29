using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.User 
{
    public partial class RegistrationViewModel : ObservableValidator, INavigatable<bool>, ICloseable
    {
        private readonly AuthService _authService;
        public event Action? RequestClose;

        // Inside RegistrationViewModel
        [ObservableProperty] private ObservableCollection<Subject> _subjects = [];
        [ObservableProperty] private ObservableCollection<Grade> _grades = [];

        [ObservableProperty]
        private Grade _selectedGrade = new Grade("hello");

        [ObservableProperty]
        private int _subjectToTeach = -1;

        [ObservableProperty]
        private int _subjectToLearn = -1;

        public RegistrationViewModel(AuthService authService)
        {
            _authService = authService;
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
            ClearErrors();

            var user = CreateUserFromState();
            var result = await _authService.Register(user);

            if (result.Success)
            {
                RequestClose?.Invoke();
            }
            else
            {
                HandleServerResult(result);
            }
        }
        partial void OnIsMentorChanged(bool value) => ValidateProperty(SubjectToTeach, nameof(SubjectToTeach));

        private Model.User CreateUserFromState()
        {
            if (SupervisorOrStudentIsSupervisor)
            {
                return new Model.Supervisor { UserName = UserName, Email = Email, NationalId = NationalId };
            }

            var student = new Model.Student { UserName = UserName, Email = Email, NationalId = NationalId, Grade = SelectedGrade };
            if (IsMentee) student.MenteeProfile = new MenteeProfile { SubjectToLearn = SubjectToLearn };
            if (IsMentor) student.MentorProfile = new MentorProfile { SubjectToTeach = SubjectToTeach };

            return student;
        }

        private void HandleServerResult(Result<Model.User> result)
        {
            if (result.ValidationErrors != null && result.ValidationErrors.Any())
            {
                ErrorMessage = string.Join(Environment.NewLine,
                    result.ValidationErrors.Select(x => $"• {x.Value}"));
            }
        }

        public Task OnNavigatedToAsync(bool supervisorOrStudentIsSupervisor)
        {
            this.SupervisorOrStudentIsSupervisor = supervisorOrStudentIsSupervisor;
            return Task.CompletedTask;
        }
    }
}