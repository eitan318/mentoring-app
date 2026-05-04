using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.Threading;
using MentoringApp.ViewModel.Localization;

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
    private CancellationTokenSource? _timerCts;

    [ObservableProperty] private string _operationResult = string.Empty;
    [ObservableProperty] private bool _hasOperationResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPhase1Active))]
    [NotifyPropertyChangedFor(nameof(IsImportStepActive))]
    private bool _isUsersImported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPhase1Active))]
    [NotifyPropertyChangedFor(nameof(IsPhase2Active))]
    private bool _isSelectionPhaseActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPhase1Active))]
    [NotifyPropertyChangedFor(nameof(IsPhase2Active))]
    private bool _isProcessComplete;

    public bool IsImportStepActive => !IsUsersImported;
    public bool IsPhase1Active => IsUsersImported && !IsSelectionPhaseActive && !IsProcessComplete;
    public bool IsPhase2Active => IsSelectionPhaseActive && !IsProcessComplete;

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

    public AdminOverviewViewModel(
        INavigationService navigationService,
        IWindowService windowService,
        UserApiClient userClient,
        MatchingApiClient matchingClient,
        SettingsApiClient settingsClient,
        IssueApiClient issueClient,
        NotificationApiClient notificationClient,
        IToastService toastService,
        ILocalizationService loc)
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
    }

    private async Task RunTimerAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                OnTimerTick();
            }
        }
        catch (OperationCanceledException) { }
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
        if (_timerCts == null || _timerCts.IsCancellationRequested)
        {
            _timerCts = new CancellationTokenSource();
            _ = RunTimerAsync(_timerCts.Token);
        }
        await LoadDataAsync();
    }
    
    public Task OnNavigatedFromAsync() { _timerCts?.Cancel(); return Task.CompletedTask; }

    private async Task LoadDataAsync()
    {
        var settings = await _settingsClient.GetAllAsync();
        if (!settings.IsSchoolConfigured)
        {
            await _navigationService.NavigateToAsync<SystemSettingsViewModel>();
            return;
        }

        IsUsersImported = settings.IsUsersImported;
        IsSelectionPhaseActive = settings.IsPhase1Complete;
        IsProcessComplete = settings.IsProcessComplete;

        ActiveDeadline = IsSelectionPhaseActive
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

        if (IsSelectionPhaseActive)
            await _settingsClient.SetPhase2DeadlineAsync(ActiveDeadline);
        else
            await _settingsClient.SetPhase1DeadlineAsync(ActiveDeadline);

        ShowResult("✓ Deadline scheduled.");
    }

    [RelayCommand]
    private async Task CancelDeadline()
    {
        ActiveDeadline = null;
        if (IsSelectionPhaseActive)
            await _settingsClient.SetPhase2DeadlineAsync(null);
        else
            await _settingsClient.SetPhase1DeadlineAsync(null);
    }

    [RelayCommand]
    private async Task MarkUsersImported()
    {
        IsUsersImported = true;
        await _settingsClient.SetIsUsersImportedAsync(true);
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
            IsSelectionPhaseActive = true;
            await _settingsClient.SetIsPhase1CompleteAsync(true);

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

            IsProcessComplete = true;
            await _settingsClient.SetIsProcessCompleteAsync(true);

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
