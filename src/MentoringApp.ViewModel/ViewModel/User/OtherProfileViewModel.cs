using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class OtherProfileViewModel : ObservableObject, INavigatable<int>
{
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string? _phoneNumber;
    [ObservableProperty] private string _genderDisplay = "";
    [ObservableProperty] private string _preferredMentorGenderDisplay = "";
    [ObservableProperty] private string _preferredMenteeGenderDisplay = "";
    [ObservableProperty] private bool _isStudent;
    [ObservableProperty] private bool _isSupervisor;
    [ObservableProperty] private string _roleName = "";
    [ObservableProperty] private string _gradeName = "";
    [ObservableProperty] private int _classNum;
    [ObservableProperty] private string _teachingSubject = "None";
    [ObservableProperty] private string _learningSubject = "None";
    [ObservableProperty] private string? _profilePicturePath;

    private readonly UserApiClient _userClient;
    private readonly ReferenceApiClient _referenceClient;

    public OtherProfileViewModel(UserApiClient userClient, ReferenceApiClient referenceClient)
    {
        _userClient = userClient;
        _referenceClient = referenceClient;
    }

    public async Task OnNavigatedToAsync(int userId)
    {
        var user = await _userClient.GetByIdAsync(userId);
        if (user == null) return;

        var subjects = (await _referenceClient.GetSubjectsAsync()).ToDictionary(s => s.Id, s => s.Name);
        var grades = (await _referenceClient.GetGradesAsync()).ToDictionary(g => g.Id, g => g.Name);

        UserName = user.UserName;
        Email = user.Email;
        ProfilePicturePath = user.ProfilePicturePath;
        PhoneNumber = user.PhoneNumber;
        GenderDisplay = GenderHelper.GenderToDisplay(user.Gender);

        if (user.IsStudent)
        {
            IsStudent = true;
            IsSupervisor = false;
            RoleName = (user.IsMentor, user.IsMentee) switch
            {
                (true, true)  => "Student · Mentor & Mentee",
                (true, false) => "Student · Mentor",
                (false, true) => "Student · Mentee",
                _             => "Student"
            };
            GradeName = user.GradeId.HasValue && grades.TryGetValue(user.GradeId.Value, out var gn) ? gn : "";
            ClassNum = user.ClassNum ?? 0;
            PreferredMentorGenderDisplay = user.PreferredMentorGender.HasValue
                ? GenderHelper.GenderPreferenceToDisplay(user.PreferredMentorGender.Value) : "";
            PreferredMenteeGenderDisplay = user.PreferredMenteeGender.HasValue
                ? GenderHelper.GenderPreferenceToDisplay(user.PreferredMenteeGender.Value) : "";

            if (user.MentorSubjectId.HasValue)
                TeachingSubject = subjects.TryGetValue(user.MentorSubjectId.Value, out var ts) ? $"Teaching: {ts}" : $"Teaching: {user.MentorSubjectId}";
            if (user.MenteeSubjectId.HasValue)
                LearningSubject = subjects.TryGetValue(user.MenteeSubjectId.Value, out var ls) ? $"Learning: {ls}" : $"Learning: {user.MenteeSubjectId}";
        }
        else if (user.IsAdmin)
        {
            IsStudent = false;
            RoleName = "Admin";
        }
        else
        {
            IsStudent = false;
            IsSupervisor = true;
            RoleName = "Supervisor";
        }
    }
}
