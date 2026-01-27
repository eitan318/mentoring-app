using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.ViewModelHelper;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.Auth
{
    public partial class RegistrationViewModel : ObservableValidator, INavigatable<bool>, ICloseable
    {
        private readonly AuthService _authService;
        public event Action? RequestClose;

        public RegistrationViewModel(AuthService authService)
        {
            _authService = authService;
        }

        [ObservableProperty] private bool _isMentor;
        [ObservableProperty] private bool _isMentee;
        [ObservableProperty] private bool _supervisorOrStudentIsSupervisor;
        [ObservableProperty] private int _subjectToTeach = -1;
        [ObservableProperty] private int _subjectToLearn = -1;
        [ObservableProperty] private int _grade = -1;
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

        private User CreateUserFromState()
        {
            if (SupervisorOrStudentIsSupervisor)
            {
                return new Supervisor { UserName = UserName, Email = Email, NationalId = NationalId };
            }
            var student = new Student { UserName = UserName, Email = Email, NationalId = NationalId, Grade = Grade };
            if (IsMentee) student.MenteeProfile = new MenteeProfile { SubjectToLearn = SubjectToLearn };
            if (IsMentor) student.MentorProfile = new MentorProfile { SubjectToTeach = SubjectToTeach };
            
            return student;
        }

        private void HandleServerResult(Result<User> result)
        {
            if (result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    var valResult = new ValidationResult(error.Value, new[] { error.Key });
                    ErrorMessage = error.Value;
                }
            }
        }

        public Task OnNavigatedToAsync(bool supervisorOrStudentIsSupervisor)
        {
            this.SupervisorOrStudentIsSupervisor = supervisorOrStudentIsSupervisor;
            return Task.CompletedTask;
        }
    }
}