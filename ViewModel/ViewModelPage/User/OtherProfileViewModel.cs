using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class OtherProfileViewModel : ObservableObject, INavigatable
    {
        [ObservableProperty] private string _userName = "";
        [ObservableProperty] private string _email = "";

        // Role Detection for UI visibility
        [ObservableProperty] private bool _isStudent;
        [ObservableProperty] private string _roleName = "";
        [ObservableProperty] private string _gradeName = "";
        [ObservableProperty] private string _teachingSubject = "None";
        [ObservableProperty] private string _learningSubject = "None";

        public OtherProfileViewModel() { }

        // This method would be called by your NavigationService when opening the profile
        public void LoadUserData(Model.User user)
        {
            if (user == null) return;

            UserName = user.UserName;
            Email = user.Email;

            if (user is Model.Student student)
            {
                IsStudent = true;
                RoleName = "Student";
                GradeName = student.Grade?.Name ?? "N/A";

                // Assuming you have a way to look up the Name from the ID
                if (student.MentorProfile != null)
                    TeachingSubject = $"Teaching: {student.MentorProfile.SubjectToTeach}";

                if (student.MenteeProfile != null)
                    LearningSubject = $"Learning: {student.MenteeProfile.SubjectToLearn}";
            }
            else if (user is Model.Supervisor)
            {
                IsStudent = false;
                RoleName = "Supervisor";
            }
        }
    }
}