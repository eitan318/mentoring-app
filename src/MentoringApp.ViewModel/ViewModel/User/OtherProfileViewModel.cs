using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class OtherProfileViewModel : ObservableObject, INavigatable<int>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStudent))]
    [NotifyPropertyChangedFor(nameof(IsSupervisor))]
    [NotifyPropertyChangedFor(nameof(HasMentorProfile))]
    [NotifyPropertyChangedFor(nameof(HasMenteeProfile))]
    [NotifyPropertyChangedFor(nameof(GradeName))]
    [NotifyPropertyChangedFor(nameof(ClassNum))]
    [NotifyPropertyChangedFor(nameof(GenderDisplay))]
    [NotifyPropertyChangedFor(nameof(PreferredMentorGenderDisplay))]
    [NotifyPropertyChangedFor(nameof(PreferredMenteeGenderDisplay))]
    private UserModel? _user;

    [ObservableProperty] private string _roleName = "";
    [ObservableProperty] private string _teachingSubject = "None";
    [ObservableProperty] private string _learningSubject = "None";
    [ObservableProperty] private bool _loading;

    public bool IsStudent => User is StudentModel;
    public bool IsSupervisor => User is SupervisorModel;
    public bool HasMentorProfile => User is StudentModel s && s.MentorProfile != null;
    public bool HasMenteeProfile => User is StudentModel s2 && s2.MenteeProfile != null;
    public string GradeName => (User as StudentModel)?.Grade?.Name ?? "";
    public int ClassNum => (User as StudentModel)?.ClassNum ?? 0;
    public string GenderDisplay => User != null ? GenderHelper.GenderToDisplay((int)User.Gender) : "";
    public string PreferredMentorGenderDisplay => User is StudentModel s
        ? GenderHelper.GenderPreferenceToDisplay((int)s.PreferredMentorGender) : "";
    public string PreferredMenteeGenderDisplay => User is StudentModel s2
        ? GenderHelper.GenderPreferenceToDisplay((int)s2.PreferredMenteeGender) : "";

    private readonly UserApiClient _userClient;
    private readonly ReferenceApiClient _referenceClient;

    public OtherProfileViewModel(UserApiClient userClient, ReferenceApiClient referenceClient)
    {
        _userClient = userClient;
        _referenceClient = referenceClient;
    }

    public async Task OnNavigatedToAsync(int userId)
    {
        Loading = true;
        try
        {
            // ה-API מחזיר StudentModel/SupervisorModel אוטומטית
            var user = await _userClient.GetByIdAsync(userId);
            if (user == null) return;

            User = user;

            // עיבוד נתונים ספציפיים לפי סוג המשתמש (Pattern Matching)
            if (User is StudentModel student)
            {
                var subjects = (await _referenceClient.GetSubjectsAsync()).ToDictionary(s => s.Id, s => s.Name);

                RoleName = (student.IsMentor, student.IsMentee) switch
                {
                    (true, true) => "Student · Mentor & Mentee",
                    (true, false) => "Student · Mentor",
                    (false, true) => "Student · Mentee",
                    _ => "Student"
                };

                if (student.MentorProfile != null)
                {
                    TeachingSubject = subjects.TryGetValue(student.MentorProfile.SubjectToTeach, out var ts)
                        ? $"Teaching: {ts}" : "Teaching: Unknown";
                }

                if (student.MenteeProfile != null)
                {
                    LearningSubject = subjects.TryGetValue(student.MenteeProfile.SubjectToLearn, out var ls)
                        ? $"Learning: {ls}" : "Learning: Unknown";
                }
            }
            else if (User is AdminModel)
            {
                RoleName = "Admin";
            }
            else if (User is SupervisorModel)
            {
                RoleName = "Supervisor";
            }
        }
        finally
        {
            Loading = false;
        }
    }
}