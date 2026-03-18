using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class MyProfileViewModel : ObservableValidator, INavigatable
    {
        private readonly AuthService _authService;
        private readonly UserStore _userStore;
        private readonly GradeService _gradeService;
        private readonly SubjectService _subjectService;
        private readonly UserService _userService;

        [ObservableProperty] private bool _isReadOnly = true;
        [ObservableProperty] private bool _isEditMode = false;

        [ObservableProperty] private string? _profilePicturePath;

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

        public MyProfileViewModel(UserStore userStore, AuthService authService, GradeService gradeService, SubjectService subjectService, UserService userService)
        {
            _userStore = userStore;
            _authService = authService;
            _gradeService = gradeService;
            _subjectService = subjectService;
            _userService = userService;
            _ = InitializeAsync();
        }
        private async Task InitializeAsync()
        {
            var grades = await _gradeService.GetAllGradesAsync();
            var subjects = await _subjectService.GetAllSubjectsAsync();

            Grades = new ObservableCollection<Grade>(grades.Data);
            Subjects = new ObservableCollection<Subject>(subjects.Data);

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

            var user = _userStore.User;
            if (user == null) return;

            // Update base user info
            user.UserName = UserName;
            user.Email = Email;

            if (user is Model.Student student)
            {
                student.Grade = SelectedGrade;
                if (HasMentorProfile && student.MentorProfile != null) student.MentorProfile.SubjectToTeach = SubjectToTeach;
                if (HasMenteeProfile && student.MenteeProfile != null) student.MenteeProfile.SubjectToLearn = SubjectToLearn;
            }

            var result = await _userService.UpdateUserAsync(user);
            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to save changes.";
                return;
            }
            ErrorMessage = string.Empty;

            IsReadOnly = true;
            IsEditMode = false;
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

            var result = await _userService.UploadProfilePictureAsync(user.Id, dialog.FileName);
            if (result.Success)
            {
                // Update in-memory user so the path persists across navigations
                user.ProfilePicturePath = _userService
                    .GetUserByIdAsync(user.Id).Result.Data?.ProfilePicturePath;
                ProfilePicturePath = user.ProfilePicturePath;
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to upload picture.";
            }
        }
    }
}