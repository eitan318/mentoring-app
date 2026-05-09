using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using MentoringApp.ViewModel.Localization;
using MentoringApp.ViewModel.Helpers;

namespace MentoringApp.ViewModel.ViewModel.Admin;

/// <summary>Summary item for the supervisor list in the admin dashboard.</summary>
public class AdminSupervisorItem
{
    public SupervisorModel Supervisor { get; }
    public int FilledStudentsCount { get; set; }
    public int TotalStudentsCount { get; set; }
    public int SupervisedPairsCount { get; set; }
    public int PendingIssuesCount { get; set; }
    public int ResolvedIssuesCount { get; set; }

    public int Id => Supervisor.Id;
    public string UserName => Supervisor.UserName;
    public string ProfilePicturePath => Supervisor.ProfilePicturePath;
    public Gender Gender => Supervisor.Gender;

    public List<SchoolClassModel> AssignedClasses { get; set; } = new();

    public double FillProgressPercent => TotalStudentsCount > 0
        ? (double)FilledStudentsCount / TotalStudentsCount * 100
        : 0;

    public string FillProgressLabel => $"{FilledStudentsCount}/{TotalStudentsCount}";

    public AdminSupervisorItem(SupervisorModel supervisor) => Supervisor = supervisor;
}

public partial class AdminOverviewViewModel : ObservableObject, INavigatable
{
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    private readonly UserApiClient _userClient;
    private readonly MatchingApiClient _matchingClient;
    private readonly SettingsApiClient _settingsClient;
    private readonly IssueApiClient _issueClient;
    private readonly NotificationApiClient _notificationClient;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;
    private readonly OneSecondTicker _ticker;
    private bool _schoolConfigSubscribed;

    public AdminProgressStore Progress { get; }
    public SchoolConfigViewModel SchoolConfig { get; }
    public SystemSettingsViewModel SupervisorAssignment { get; }

    [ObservableProperty] private string _operationResult = string.Empty;
    [ObservableProperty] private bool _hasOperationResult;

    public string Phase1Summary => _loc.Get("Admin_Phase1_Summary");
    public string Phase2Summary => _loc.Get("Admin_Phase2_Summary");

    [ObservableProperty] private DateTime? _deadlineInput = DateTime.Now.AddDays(1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeadlineScheduled))]
    [NotifyPropertyChangedFor(nameof(IsDeadlineNotScheduled))]
    private DateTime? _activeDeadline;

    [ObservableProperty] private string _deadlineTimeRemaining = string.Empty;

    public bool IsDeadlineScheduled => ActiveDeadline.HasValue;
    public bool IsDeadlineNotScheduled => !ActiveDeadline.HasValue;

    public ObservableCollection<AdminSupervisorItem> SupervisorsListPreview { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasForwardedIssues))]
    private ObservableCollection<IssueModel> _forwardedIssues = [];

    public bool HasForwardedIssues => ForwardedIssues.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedForwardedIssuePane))]
    private object? _selectedForwardedIssuePane;

    public bool HasSelectedForwardedIssuePane => SelectedForwardedIssuePane != null;

    [ObservableProperty] private double _totalFillPercent;
    [ObservableProperty] private string _totalFillLabel = string.Empty;
    [ObservableProperty] private bool _canCompleteSchoolConfig;

    public AdminOverviewViewModel(
        INavigationService navigationService,
        IWindowService windowService,
        UserApiClient userClient,
        MatchingApiClient matchingClient,
        SettingsApiClient settingsClient,
        IssueApiClient issueClient,
        NotificationApiClient notificationClient,
        IToastService toastService,
        ILocalizationService loc,
        AdminProgressStore progress,
        SchoolConfigViewModel schoolConfig,
        SystemSettingsViewModel supervisorAssignment)
    {
        _navigationService = navigationService;
        _windowService = windowService;
        _userClient = userClient;
        _matchingClient = matchingClient;
        _settingsClient = settingsClient;
        _issueClient = issueClient;
        _notificationClient = notificationClient;
        _toastService = toastService;
        _loc = loc;
        Progress = progress;
        SchoolConfig = schoolConfig;
        SupervisorAssignment = supervisorAssignment;
        _ticker = new OneSecondTicker(OnTimerTick);
    }

    private void OnTimerTick()
    {
        if (!ActiveDeadline.HasValue) { DeadlineTimeRemaining = string.Empty; return; }
        var remaining = ActiveDeadline.Value - DateTime.Now;
        DeadlineTimeRemaining = remaining.TotalSeconds <= 0
            ? _loc.Get("Admin_Timer_Pending")
            : $"{remaining.Days}d {remaining.Hours:D2}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s";
    }

    [RelayCommand]
    private void ShowPhaseInfo(string phase)
    {
        string title = TranslationSource.Instance["Admin_PhaseGuide_Title"] ?? "Phase Guide";
        string body = "";

        switch (phase)
        {
            case "Import":
                body = TranslationSource.Instance["Admin_PhaseGuide_Import"] ?? "Import Users";
                break;
            case "SchoolConfig":
                body = TranslationSource.Instance["Admin_PhaseGuide_SchoolConfig"] ?? "Configure your school's grades and classes.";
                break;
            case "Phase1":
                body = TranslationSource.Instance["Admin_PhaseGuide_Phase1"] ?? "Phase 1: Registration";
                break;
            case "Phase2":
                body = TranslationSource.Instance["Admin_PhaseGuide_Phase2"] ?? "Phase 2: Selection";
                break;
            case "Phase3":
                body = TranslationSource.Instance["Admin_PhaseGuide_Phase3"] ?? "Phase 3: Active Mentoring";
                break;
            default:
                return;
        }

        _windowService.ShowMessage(body, title);
    }

    public async Task OnNavigatedToAsync()
    {
        _ticker.Start();
        await LoadDataAsync();
    }

    public Task OnNavigatedFromAsync() { _ticker.Stop(); return Task.CompletedTask; }

    private async Task LoadDataAsync()
    {
        await Progress.RefreshAsync();
        await SchoolConfig.LoadAsync();

        UpdateSchoolConfigCompletion();
        if (!_schoolConfigSubscribed)
        {
            SchoolConfig.AllClasses.CollectionChanged += (s, e) => UpdateSchoolConfigCompletion();
            _schoolConfigSubscribed = true;
        }

        if (!Progress.IsSchoolConfigured) return;

        await SupervisorAssignment.RefreshSupervisorAssignmentsAsync();

        var settings = await _settingsClient.GetAllAsync();

        ActiveDeadline = Progress.IsSelectionPhaseActive
            ? (settings.Phase2Deadline != null ? DateTime.Parse(settings.Phase2Deadline) : null)
            : (settings.Phase1Deadline != null ? DateTime.Parse(settings.Phase1Deadline) : null);

        if (ActiveDeadline.HasValue) DeadlineInput = ActiveDeadline;

        var allUsers = await _userClient.GetAllAsync();
        var allStudents = allUsers.OfType<StudentModel>().ToList();

        SupervisorsListPreview.Clear();
        int globalFilled = allStudents.Count(s => IsStudentInfoFilled(s));
        int globalTotal = allStudents.Count;

        var supervisorStats = await _userClient.GetSupervisorStatsAsync();
        var statsMap = supervisorStats.ToDictionary(s => s.Id);

        foreach (var supervisor in allUsers.OfType<SupervisorModel>())
        {
            statsMap.TryGetValue(supervisor.Id, out var stats);

            var supervisorStudents = allStudents.Where(s =>
                s.Grade != null &&
                supervisor.AssignedClasses.Any(ac => ac.Grade?.Id == s.Grade.Id && ac.ClassNum == s.ClassNum)
            ).ToList();

            int supervisorFilled = supervisorStudents.Count(s => IsStudentInfoFilled(s));
            int supervisorTotal = supervisorStudents.Count;

            var item = new AdminSupervisorItem(supervisor)
            {
                FilledStudentsCount  = supervisorFilled,
                TotalStudentsCount   = supervisorTotal,
                SupervisedPairsCount = stats?.PairsCount ?? 0,
                PendingIssuesCount   = stats?.PendingIssuesCount ?? 0,
                ResolvedIssuesCount  = stats?.ResolvedIssuesCount ?? 0,
                AssignedClasses      = supervisor.AssignedClasses.ToList(),
            };

            SupervisorsListPreview.Add(item);
        }

        TotalFillPercent = globalTotal > 0 ? (double)globalFilled / globalTotal * 100 : 0;
        TotalFillLabel = $"{globalFilled}/{globalTotal} ({(int)TotalFillPercent}%)";

        var forwarded = await _issueClient.GetForwardedAsync();
        ForwardedIssues = new ObservableCollection<IssueModel>(forwarded);
    }

    [RelayCommand]
    private async Task SaveDeadline()
    {
        if (!DeadlineInput.HasValue) return;
        ActiveDeadline = DeadlineInput.Value;

        if (Progress.IsSelectionPhaseActive)
            await _settingsClient.SetPhase2DeadlineAsync(ActiveDeadline);
        else
            await _settingsClient.SetPhase1DeadlineAsync(ActiveDeadline);

        ShowResult("✓ Deadline scheduled.");
    }

    [RelayCommand]
    private async Task CancelDeadline()
    {
        ActiveDeadline = null;
        if (Progress.IsSelectionPhaseActive)
            await _settingsClient.SetPhase2DeadlineAsync(null);
        else
            await _settingsClient.SetPhase1DeadlineAsync(null);
    }

    [RelayCommand]
    private async Task CompleteSchoolConfig()
    {
        if (SchoolConfig.AllClasses.Count == 0)
        {
            _toastService.Error(_loc.Get("Admin_SchoolConfig_NoClasses_Error"));
            return;
        }
        await Progress.MarkSchoolConfiguredAsync();
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task CompleteSupervisorAssignment()
    {
        if (!SupervisorAssignment.AllClassesAssigned)
        {
            _toastService.Error(_loc.Get("Admin_SupervisorAssignment_Incomplete_Error"));
            return;
        }
        await Progress.MarkSupervisorsAssignedAsync();
        try
        {
            await _notificationClient.SendPhase1StartedAsync();
        }
        catch
        {
            await _toastService.ShowInfoAsync(
                _loc.Get("Admin_EmailNotification_Failed_Title"),
                _loc.Get("Admin_EmailNotification_Failed_Body"));
        }
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task MarkUsersImported()
    {
        await Progress.MarkUsersImportedAsync();
    }

    [RelayCommand]
    private async Task StartSelectionPhase()
    {
        if (!await _windowService.ShowConfirmAsync(
            _loc.Get("Admin_ConfirmStartPhase2_Body"),
            _loc.Get("Admin_ConfirmAction_Title"))) return;

        ShowResult(_loc.Get("Admin_GeneratingScores_Message"));

        try
        {
            await _matchingClient.GenerateScoresAsync();
            await Progress.MarkSelectionPhaseActiveAsync();

            var settings = await _settingsClient.GetAllAsync();
            ActiveDeadline = settings.Phase2Deadline != null ? DateTime.Parse(settings.Phase2Deadline) : null;
            DeadlineInput = DateTime.Now.AddDays(1);

            try { await _notificationClient.SendPhase2StartedAsync(); }
            catch
            {
                await _toastService.ShowInfoAsync(
                    _loc.Get("Admin_EmailNotification_Failed_Title"),
                    _loc.Get("Admin_EmailNotification_Failed_Body"));
            }
            await LoadDataAsync();
            ShowResult(_loc.Get("Admin_ScoresGenerated_Message"));
        }
        catch (Exception ex)
        {
            ShowResult(_loc.Format("Admin_Phase2Failed_Message", ex.Message));
        }
    }

    [RelayCommand]
    private async Task RunAutoMatch()
    {
        if (!await _windowService.ShowConfirmAsync(
            _loc.Get("Admin_ConfirmAutoMatch_Body"),
            _loc.Get("Admin_ConfirmAction_Title"))) return;

        ShowResult(_loc.Get("Admin_RunningAutoMatch_Message"));

        try
        {
            var result = await _matchingClient.RunAutoMatchAsync();
            var fallback = await _matchingClient.RunFallbackMatchAsync();
            string fallbackText = _loc.Format("Admin_FallbackAssigned_Text", fallback.PairsCreated);

            await Progress.MarkProcessCompleteAsync();
            await LoadDataAsync();

            ShowResult(_loc.Format("Admin_ProcessComplete_Message", result.PairsCreated, fallbackText));
        }
        catch (Exception ex)
        {
            ShowResult(_loc.Format("Admin_AutoMatchFailed_Message", ex.Message));
        }
    }

    [RelayCommand]
    private async Task ShowPhase1Info() =>
        await _toastService.ShowInfoAsync(_loc.Get("Admin_Phase1Info_Title"), _loc.Get("Admin_Phase1Info_Body"));

    [RelayCommand]
    private async Task ShowPhase2Info() =>
        await _toastService.ShowInfoAsync(_loc.Get("Admin_Phase2Info_Title"), _loc.Get("Admin_Phase2Info_Body"));

    [RelayCommand]
    private async Task SelectForwardedIssue(IssueModel? issue)
    {
        if (issue == null) { SelectedForwardedIssuePane = null; return; }
        var vm = new IssueViewModel(_navigationService, _issueClient);
        vm.OnCloseRequested = () => SelectedForwardedIssuePane = null;
        vm.OnIssueResolved = () => { SelectedForwardedIssuePane = null; _ = LoadDataAsync(); };
        await vm.OnNavigatedToAsync(issue.Id);
        SelectedForwardedIssuePane = vm;
    }

    [RelayCommand] private async Task ManageUsers() => await _navigationService.NavigateToAsync<ManageUsersViewModel>();

    [RelayCommand]
    private async Task InspectSupervisor(AdminSupervisorItem chosen)
    {
        if (chosen != null)
            await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(chosen.Id);
    }

    private void ShowResult(string message)
    {
        OperationResult = message;
        HasOperationResult = true;
    }

    private void UpdateSchoolConfigCompletion()
    {
        CanCompleteSchoolConfig = SchoolConfig.AllClasses.Count > 0;
    }

    internal static bool IsStudentInfoFilled(StudentModel s)
    {
        if (!s.IsMentor && !s.IsMentee) return false;
        if (s.IsMentor && (s.MentorProfile == null || s.MentorProfile.SubjectToTeach <= 0))
            return false;
        if (s.IsMentee && (s.MenteeProfile == null || s.MenteeProfile.SubjectToLearn <= 0))
            return false;
        return true;
    }
}
