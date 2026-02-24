using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class MyProfileViewModel : ObservableValidator, INavigatable
    {
        private readonly AuthService _authService;
        private readonly UserStore _userStore;

        [ObservableProperty] private bool _isReadOnly = true;
        [ObservableProperty] private bool _isEditMode = false;

        [ObservableProperty] private ObservableCollection<Subject> _subjects = [];
        [ObservableProperty] private ObservableCollection<Grade> _grades = [];
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

        // Editable Student Data
        [ObservableProperty] private Grade _selectedGrade;
        [ObservableProperty] private int _subjectToTeach = -1;
        [ObservableProperty] private int _subjectToLearn = -1;

        public MyProfileViewModel(UserStore userStore, AuthService authService)
        {
            _userStore = userStore;
            _authService = authService;
            LoadUserData();
        }

        private void LoadUserData()
        {
            var user = _userStore.User;
            if (user == null) return;

            UserName = user.UserName;
            Email = user.Email;
            NationalId = user.NationalId;

            if (user is Model.Student student)
            {
                IsSupervisor = false;
                SelectedGrade = student.Grade;
                HasMentorProfile = student.MentorProfile != null;
                HasMenteeProfile = student.MenteeProfile != null;

                if (HasMentorProfile) SubjectToTeach = student.MentorProfile.SubjectToTeach;
                if (HasMenteeProfile) SubjectToLearn = student.MenteeProfile.SubjectToLearn;
            }
            else { IsSupervisor = true; }
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

            // Update Logic
            if (_userStore.User is Model.Student student)
            {
                student.UserName = UserName;
                student.Email = Email;
                student.Grade = SelectedGrade;
                if (HasMentorProfile) student.MentorProfile.SubjectToTeach = SubjectToTeach;
                if (HasMenteeProfile) student.MenteeProfile.SubjectToLearn = SubjectToLearn;
            }

            IsReadOnly = true;
            IsEditMode = false;
        }
    }
}