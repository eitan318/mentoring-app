using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage
{
    public class RegistrationViewModel : ViewModelBase, ICloseable
    {
        private readonly AuthService _authService;

        public event Action? RequestClose;
        
        public ICommand RegisterCommand { get; }


        private UserRole _registringUserRole = UserRole.InvalidUserRole;
        public UserRole RegisteringUserRole { get => _registringUserRole; set => SetProperty(ref _registringUserRole, value); }

        private int _subjectToTeach = -1;
        public int SubjectToTeach { get => _subjectToTeach; set => SetProperty(ref _subjectToTeach, value); }

        private int _subjectToLearn = -1;
        public int SubjectToLearn { get => _subjectToLearn; set => SetProperty(ref _subjectToLearn, value); }

        private int _grade = -1;
        public int Grade { get => _grade; set => SetProperty(ref _grade, value); }

        private string _email = "";
        public string Email { get => _email; set => SetProperty(ref _email, value); }

        private string _userName = "";
        public string UserName { get => _userName; set => SetProperty(ref _userName, value); }

        private string _nationalId = "";
        public string NationalId { get => _nationalId; set => SetProperty(ref _nationalId, value); }

        private string _errorMessage = "";
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        public RegistrationViewModel(AuthService authService)
        {
            _authService = authService;
            
            // Point the command to the local method
            RegisterCommand = new RelayCommand(Register);
        }

        private void Register()
        {
            // Use the C# switch expression correctly
            User user = RegisteringUserRole switch
            {
                UserRole.Supervisor => new Supervisor 
                { 
                    UserName = UserName, 
                    Email = Email, 
                    NationalId = NationalId 
                },
                UserRole.Mentee => new Student 
                { 
                    UserName = UserName, 
                    Email = Email, 
                    NationalId = NationalId, 
                    Grade = Grade,
                    // Ensure the Profile object is created
                    MenteeProfile = new MenteeProfile { SubjectToLearn = SubjectToLearn } 
                },
                UserRole.Mentor => new Student 
                { 
                    UserName = UserName, 
                    Email = Email, 
                    NationalId = NationalId, 
                    Grade = Grade,
                    // Ensure the Profile object is created
                    MentorProfile = new MentorProfile { SubjectToTeach = SubjectToTeach } 
                },
                _ => throw new System.NotImplementedException("Role not supported")
            };

            if (!_authService.Register(user))
            {
                ErrorMessage = "Could not register";
            }
            else
            {
                ErrorMessage = ""; 
                RequestClose?.Invoke();
            }
        }
    }
}