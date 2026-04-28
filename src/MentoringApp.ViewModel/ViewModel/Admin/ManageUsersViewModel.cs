using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public partial class ManageUsersViewModel : ObservableObject, INavigatable
{
    private readonly IFileService _fileService;
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;
    private readonly UserApiClient _userClient;
    private readonly PairApiClient _pairClient;

    public ManageUsersViewModel(
        IFileService fileService,
        IWindowService windowService,
        INavigationService navigationService,
        IToastService toastService,
        ILocalizationService loc,
        UserApiClient userClient,
        PairApiClient pairClient)
    {
        _fileService = fileService;
        _windowService = windowService;
        _navigationService = navigationService;
        _toastService = toastService;
        _loc = loc;
        _userClient = userClient;
        _pairClient = pairClient;
    }

    // --- Properties & Collections ---
    public ObservableCollection<UserModel> AllUsers { get; set; } = [];
    public ObservableCollection<string> AvailableGrades { get; set; } = [];
    private HashSet<int> _pairedUserIds = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
    [NotifyCanExecuteChangedFor(nameof(ViewUserCommand))]
    private UserModel? _selectedUser;

    private bool HasSelectedUser => SelectedUser != null;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedRole = "All";
    public static IReadOnlyList<string> RoleOptions { get; } = ["All", "Supervisor", "Student"];
    public bool IsStudentFilterVisible => SelectedRole == "Student";

    [ObservableProperty] private bool _filterIsMentor;
    [ObservableProperty] private bool _filterIsMentee;
    [ObservableProperty] private bool _filterUnpairedOnly;
    [ObservableProperty] private string? _filterMinGrade;
    [ObservableProperty] private string? _filterMaxGrade;

    [ObservableProperty] private bool _isStudentInfoVisible;
    [ObservableProperty] private bool _isSupervisorInfoVisible;

    // --- Logic & Filtering ---
    public IEnumerable<UserModel> FilteredUsers
    {
        get
        {
            var q = AllUsers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(u => u.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                 u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            q = SelectedRole switch
            {
                "Supervisor" => q.OfType<SupervisorModel>(),
                "Student" => q.OfType<StudentModel>(),
                _ => q
            };

            if (SelectedRole == "Student")
            {
                var students = q.OfType<StudentModel>();

                if (FilterIsMentor || FilterIsMentee)
                    students = students.Where(s => FilterIsMentor && FilterIsMentee ? s.IsMentor && s.IsMentee
                                                 : FilterIsMentor ? s.IsMentor : s.IsMentee);

                if (FilterUnpairedOnly)
                    students = students.Where(s => !_pairedUserIds.Contains(s.Id));

                if (!string.IsNullOrEmpty(FilterMinGrade) && int.TryParse(FilterMinGrade, out int min))
                    students = students.Where(s => s.Grade != null && s.Grade.Id >= min);

                if (!string.IsNullOrEmpty(FilterMaxGrade) && int.TryParse(FilterMaxGrade, out int max))
                    students = students.Where(s => s.Grade != null && s.Grade.Id <= max);

                return students;
            }

            return q;
        }
    }

    // --- Lifecycle ---
    public async Task OnNavigatedToAsync()
    {
        var users = await _userClient.GetAllAsync();
        AllUsers = new ObservableCollection<UserModel>(users);

        var pairs = await _pairClient.GetAllAsync();
        _pairedUserIds = pairs.SelectMany(p => new[] { p.Mentor.Id, p.Mentee.Id }).ToHashSet();

        var grades = AllUsers.OfType<StudentModel>()
                             .Where(s => s.Grade != null)
                             .Select(s => s.Grade!.Id.ToString())
                             .Distinct()
                             .OrderBy(x => int.Parse(x))
                             .ToList();

        AvailableGrades.Clear();
        foreach (var g in grades) AvailableGrades.Add(g);

        FilterMinGrade = null;
        FilterMaxGrade = null;
        OnPropertyChanged(nameof(FilteredUsers));
    }

    // --- Property Change Handlers ---
    partial void OnSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredUsers));
    partial void OnSelectedRoleChanged(string v) { OnPropertyChanged(nameof(IsStudentFilterVisible)); OnPropertyChanged(nameof(FilteredUsers)); }
    partial void OnFilterIsMentorChanged(bool v) => OnPropertyChanged(nameof(FilteredUsers));
    partial void OnFilterIsMenteeChanged(bool v) => OnPropertyChanged(nameof(FilteredUsers));
    partial void OnFilterUnpairedOnlyChanged(bool v) => OnPropertyChanged(nameof(FilteredUsers));
    partial void OnFilterMinGradeChanged(string? v) => OnPropertyChanged(nameof(FilteredUsers));
    partial void OnFilterMaxGradeChanged(string? v) => OnPropertyChanged(nameof(FilteredUsers));

    // --- Commands ---
    [RelayCommand] private async Task RegisterStudent() => await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(false);
    [RelayCommand] private async Task RegisterSupervisor() => await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(true);
    [RelayCommand] private void ToggleStudentInfo() => IsStudentInfoVisible = !IsStudentInfoVisible;
    [RelayCommand] private void ToggleSupervisorInfo() => IsSupervisorInfoVisible = !IsSupervisorInfoVisible;
    [RelayCommand] private void CloseStudentInfo() => IsStudentInfoVisible = false;
    [RelayCommand] private void CloseSupervisorInfo() => IsSupervisorInfoVisible = false;

    [RelayCommand(CanExecute = nameof(HasSelectedUser))]
    private async Task DeleteUser()
    {
        if (SelectedUser is null) return;
        bool confirmed = await _toastService.ConfirmAsync(_loc.Get("Confirm_DeleteUser_Title"), _loc.Get("Confirm_DeleteUser_Body"));
        if (!confirmed) return;

        await _userClient.DeleteAsync(SelectedUser.Id);
        AllUsers.Remove(SelectedUser);
        SelectedUser = null;
        OnPropertyChanged(nameof(FilteredUsers));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedUser))]
    private async Task ViewUser()
    {
        if (SelectedUser != null)
            await _navigationService.NavigateToAsync<OtherProfileViewModel, int>(SelectedUser.Id);
    }

    [RelayCommand]
    private async Task ImportStudents()
    {
        IsStudentInfoVisible = false;
        string? file = _fileService.OpenFile("Excel files (*.xlsx)|*.xlsx");
        if (string.IsNullOrEmpty(file)) return;
        try
        {
            int count = await _userClient.ImportStudentsAsync(file);
            _toastService.Success(_loc.Format("ManageUsers_ImportStudents_Success", count));
            await OnNavigatedToAsync();
        }
        catch (Exception ex) { _toastService.Error(_loc.Format("ManageUsers_ImportStudents_Error", ex.Message)); }
    }

    [RelayCommand]
    private async Task ImportSupervisors()
    {
        IsSupervisorInfoVisible = false;
        string? file = _fileService.OpenFile("Excel files (*.xlsx)|*.xlsx");
        if (string.IsNullOrEmpty(file)) return;
        try
        {
            int count = await _userClient.ImportSupervisorsAsync(file);
            _toastService.Success(_loc.Format("ManageUsers_ImportSupervisors_Success", count));
            await OnNavigatedToAsync();
        }
        catch (Exception ex) { _toastService.Error(_loc.Format("ManageUsers_ImportSupervisors_Error", ex.Message)); }
    }

    [RelayCommand]
    private async Task DownloadStudentTemplate()
    {
        string? savePath = _fileService.SaveFile("Excel files (*.xlsx)|*.xlsx", "students_import_template.xlsx");
        if (string.IsNullOrEmpty(savePath)) return;
        try { await _userClient.DownloadTemplateAsync(false, savePath); _toastService.Success(_loc.Get("ManageUsers_TemplateSaved_Success")); }
        catch (Exception ex) { _toastService.Error(_loc.Format("ManageUsers_TemplateSaved_Error", ex.Message)); }
    }

    [RelayCommand]
    private async Task DownloadSupervisorTemplate()
    {
        string? savePath = _fileService.SaveFile("Excel files (*.xlsx)|*.xlsx", "supervisors_import_template.xlsx");
        if (string.IsNullOrEmpty(savePath)) return;
        try { await _userClient.DownloadTemplateAsync(true, savePath); _windowService.ShowMessage(_loc.Get("ManageUsers_TemplateSaved_Success"), _loc.Get("ManageUsers_DownloadComplete_Title")); }
        catch (Exception ex) { _windowService.ShowMessage(_loc.Format("ManageUsers_TemplateSaved_Error", ex.Message), _loc.Get("Common_Error_Title")); }
    }
}