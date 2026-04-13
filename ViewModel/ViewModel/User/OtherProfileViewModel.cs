using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User
{
    public partial class OtherProfileViewModel : ObservableObject, INavigatable<int>
    {
        [ObservableProperty] private string _userName = "";
        [ObservableProperty] private string _email = "";
        [ObservableProperty] private string? _phoneNumber;
        [ObservableProperty] private string _genderDisplay = "";
        [ObservableProperty] private string _preferredMentorGenderDisplay = "";
        [ObservableProperty] private string _preferredMenteeGenderDisplay = "";

        // Role Detection for UI visibility
        [ObservableProperty] private bool _isStudent;
        [ObservableProperty] private bool _isSupervisor;
        [ObservableProperty] private string _roleName = "";
        [ObservableProperty] private string _gradeName = "";
        [ObservableProperty] private int _classNum;
        [ObservableProperty] private string _teachingSubject = "None";
        [ObservableProperty] private string _learningSubject = "None";
        [ObservableProperty] private string? _profilePicturePath;
        [ObservableProperty] private Gender _gender = Gender.PreferNoAnswer;

        private readonly UserService _userService;

        public OtherProfileViewModel(UserService userService) { 
            this._userService = userService;
        }

        public async Task OnNavigatedToAsync(int userId)
        {
            UserModel user = _userService.GetUserByIdAsync(userId).Result.Data;
            await LoadUserData(user);
        }

        private async Task LoadUserData(UserModel user)
        {
            if (user == null) return;

            UserName = user.UserName;
            Email = user.Email;
            ProfilePicturePath = user.ProfilePicturePath;
            PhoneNumber = user.PhoneNumber;
            Gender = user.Gender;
            GenderDisplay = GenderHelper.GenderToDisplay(user.Gender);

            if (user is StudentModel student)
            {
                IsStudent = true;
                RoleName = student switch
                {
                    { IsMentor: true, IsMentee: true } => "Student · Mentor & Mentee",
                    { IsMentor: true }                 => "Student · Mentor",
                    { IsMentee: true }                 => "Student · Mentee",
                    _                                  => "Student"
                };
                GradeName = student.Grade?.Name ?? "";
                ClassNum = student.ClassNum;
                PreferredMentorGenderDisplay = GenderHelper.GenderPreferenceToDisplay(student.PreferredMentorGender);
                PreferredMenteeGenderDisplay = GenderHelper.GenderPreferenceToDisplay(student.PreferredMenteeGender);

                // Assuming you have a way to look up the Name from the ID
                if (student.MentorProfile != null)
                    TeachingSubject = $"Teaching: {student.MentorProfile.SubjectToTeach}";

                if (student.MenteeProfile != null)
                    LearningSubject = $"Learning: {student.MenteeProfile.SubjectToLearn}";
            }
            else if (user is AdminModel)
            {
                IsStudent = false;
                RoleName = "Admin";
            }
            else if (user is SupervisorModel)
            {
                IsStudent = false;
                RoleName = "Supervisor";
            }
        }
    }
}