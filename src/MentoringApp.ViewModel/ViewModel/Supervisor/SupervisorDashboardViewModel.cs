using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.Threading;

namespace MentoringApp.ViewModel.ViewModel.Supervisor;

public partial class SupervisorDashboardViewModel : ObservableObject, INavigatable<int>
{
    protected readonly INavigationService _navigationService;
    protected readonly PairApiClient _pairClient;
    protected readonly IssueApiClient _issueClient;
    protected readonly UserApiClient _userClient;
    protected readonly ReviewApiClient _reviewClient;
    protected readonly SettingsApiClient _settingsClient;
    protected readonly ReferenceApiClient _referenceClient;
    private readonly UserStore _userStore;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    private int _currentSupervisorId;

    [ObservableProperty] private UserModel? _selectedSupervisor;
    [ObservableProperty] private ObservableCollection<PairProgressItem> _pairsSupervised = [];
    [ObservableProperty] private ObservableCollection<UserModel> _inactiveStudents = [];
    [ObservableProperty] private string _classProgressInfo = string.Empty;
    [ObservableProperty] private double _classProgressPercent = 0.0;

    [ObservableProperty] private bool _isPhaseBannerVisible;
    [ObservableProperty] private string _bannerTitle = string.Empty;
    [ObservableProperty] private string _bannerSubtitle = string.Empty;
    [ObservableProperty] private string _bannerTimer = string.Empty;
    [ObservableProperty] private string _bannerColor = "#E3F2FD";
    [ObservableProperty] private string _bannerTextColor = "#1565C0";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArePairsAndIssuesVisible))]
    private bool _isPhase1Active;
    [ObservableProperty] private bool _isPhase2Active;

    public bool ArePairsAndIssuesVisible => !IsPhase1Active;

    private CancellationTokenSource? _timerCts;
    private DateTime? _tier1Deadline;
    private DateTime? _tier3Deadline;
    private bool _isPhase1Complete;
    private bool _isProcessComplete;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
    [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
    [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
    private ObservableCollection<IssueModel> _allIssues = [];

    private int? _issueFilterMentorId;
    private int? _issueFilterMenteeId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
    [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
    [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
    [NotifyPropertyChangedFor(nameof(IssuesSectionContextTitle))]
    private PairProgressItem? _issueFilterPair;

    public string IssuesSectionContextTitle => IssueFilterPair == null
        ? "Issues"
        : $"Issues of {IssueFilterPair.Pair.Mentor.UserName} & {IssueFilterPair.Pair.Mentee.UserName}";

    partial void OnIssueFilterPairChanged(PairProgressItem? value)
    {
        _issueFilterMentorId = value?.Pair.Mentor.Id;
        _issueFilterMenteeId = value?.Pair.Mentee.Id;
    }

    public IEnumerable<IssueModel> FilteredPendingIssues => AllIssues
        .Where(i => !i.IsResolved)
        .Where(i => IssueFilterPair == null ||
            i.ReportedByUserId == _issueFilterMentorId ||
            i.ReportedByUserId == _issueFilterMenteeId);

    public IEnumerable<IssueModel> FilteredResolvedIssues => AllIssues
        .Where(i => i.IsResolved)
        .Where(i => IssueFilterPair == null ||
            i.ReportedByUserId == _issueFilterMentorId ||
            i.ReportedByUserId == _issueFilterMenteeId);

    public int ResolvedIssuesCount => FilteredResolvedIssues.Count();

    [ObservableProperty] private object? _selectedPaneContent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingPending))]
    [NotifyPropertyChangedFor(nameof(IsShowingResolved))]
    private bool _showResolvedIssues;

    public bool IsShowingPending  => !ShowResolvedIssues;
    public bool IsShowingResolved =>  ShowResolvedIssues;

    [RelayCommand] private void ShowPending()  => ShowResolvedIssues = false;
    [RelayCommand] private void ShowResolved() => ShowResolvedIssues = true;

    [ObservableProperty] private ObservableCollection<PairProgressItem> _incompleteProfilePairs = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIncompleteProfileWarnings))]
    private int _incompleteProfileCount;

    public bool HasIncompleteProfileWarnings => IncompleteProfileCount > 0;

    public bool IsAdminViewing => _userStore.User?.IsAdmin == true;

    public SupervisorDashboardViewModel(
        INavigationService navigationService,
        PairApiClient pairClient,
        IssueApiClient issueClient,
        UserApiClient userClient,
        ReviewApiClient reviewClient,
        SettingsApiClient settingsClient,
        ReferenceApiClient referenceClient,
        UserStore userStore,
        IToastService toastService,
        ILocalizationService loc)
    {
        _navigationService = navigationService;
        _pairClient = pairClient;
        _issueClient = issueClient;
        _userClient = userClient;
        _reviewClient = reviewClient;
        _settingsClient = settingsClient;
        _referenceClient = referenceClient;
        _userStore = userStore;
        _toastService = toastService;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync()
    {
        // If no supervisor ID is set, try to use the current logged-in user's ID.
        // This handles cases where a supervisor navigates back to their own dashboard.
        int supervisorId = _currentSupervisorId > 0 ? _currentSupervisorId : _userStore.User?.Id ?? 0;
        if (supervisorId <= 0) return;

        await LoadSupervisorDataAsync(supervisorId);
    }


    private async Task LoadSupervisorDataAsync(int supervisorId)
    {
        var pairs = await _pairClient.GetBySupervisorAsync(supervisorId);
        var issues = await _issueClient.GetBySupervisorAsync(supervisorId);
        var settings = await _settingsClient.GetAllAsync();

        double requiredHours = settings.MeetingHoursBarrier;

        var reviewTasks = pairs.Select(pair =>
            _reviewClient.GetByPairAsync(pair.Id).ContinueWith(t =>
                (pair, hours: t.Result.Sum(r => r.AmountOfHours))));

        var reviewResults = await Task.WhenAll(reviewTasks);

        var progressItems = reviewResults
            .Select(r => new PairProgressItem(r.pair, r.hours, requiredHours))
            .ToList();

        PairsSupervised = new ObservableCollection<PairProgressItem>(
            progressItems.OrderBy(p => p.TotalMeetingHours));

        var warnings = progressItems
            .Where(p => p.IsProfileIncomplete || p.MatchTier == Model.MatchTier.FallbackRandom)
            .ToList();

        IncompleteProfilePairs = new ObservableCollection<PairProgressItem>(warnings);
        IncompleteProfileCount = warnings.Count;

        AllIssues = new ObservableCollection<IssueModel>(issues);

        // Get supervisor's assigned classes
        var supervisorClasses = await _referenceClient.GetSchoolClassesBySupervisorAsync(supervisorId);
        var assignedSlots = supervisorClasses.Select(c => (c.Grade.Id, c.ClassNum)).ToHashSet();

        // Load students in the supervisor's assigned classes to track registration progress
        var myStudents = (await _userClient.GetStudentsBySupervisorAsync(supervisorId))
            .OfType<StudentModel>()
            .ToList();

        int totalStudents = myStudents.Count;
        var inactive = myStudents.Where(s => !IsStudentInfoFilled(s)).ToList();

        InactiveStudents = new ObservableCollection<UserModel>(inactive);

        if (totalStudents > 0)
        {
            int registered = totalStudents - inactive.Count;
            ClassProgressPercent = (double)registered / totalStudents * 100;

            string classLabel = assignedSlots.Count == 1
                ? $"Class {assignedSlots.First().ClassNum}"
                : $"{assignedSlots.Count} classes";

            ClassProgressInfo = $"{classLabel}: {(int)ClassProgressPercent}% complete ({registered}/{totalStudents} registered)";
        }
        else
        {
            ClassProgressPercent = 0;
            ClassProgressInfo = "No students found in your assigned classes.";
        }
    }


    [RelayCommand]
    private async Task SelectIssue(IssueModel? issue)
    {
        if (issue != null)
        {
            var vm = new IssueViewModel(_navigationService, _issueClient);
            var item = PairsSupervised.FirstOrDefault(p =>
                p.Pair.Mentor.Id == issue.ReportedByUserId || p.Pair.Mentee.Id == issue.ReportedByUserId);
            if (item != null)
                vm.RelatedPairName = _loc.Format("Supervisor_RelatedPairName_Format", item.Pair.Mentor.UserName, item.Pair.Mentee.UserName);
            vm.ForwardingsupervisorId = _currentSupervisorId;
            vm.OnCloseRequested = () => SelectedPaneContent = null;
            vm.OnIssueResolved = () => { SelectedPaneContent = null; _ = LoadSupervisorDataAsync(_currentSupervisorId); };
            vm.OnIssueForwarded = () => { _ = LoadSupervisorDataAsync(_currentSupervisorId); };
            await vm.OnNavigatedToAsync(issue.Id);
            SelectedPaneContent = vm;
        }
    }

    [RelayCommand]
    private async Task SelectPair(PairProgressItem? item)
    {
        IssueFilterPair = item;

        if (item != null)
        {
            var vm = new PairDetailsViewModel(_pairClient, _issueClient, _reviewClient, _settingsClient)
            {
                ShowIssues = false
            };
            await vm.OnNavigatedToAsync(item.Id);
            SelectedPaneContent = vm;
        }
        else
        {
            SelectedPaneContent = null;
        }
    }


    private async Task LoadMatchingSettingsAsync()
    {
        var settings = await _settingsClient.GetAllAsync();
        _tier1Deadline = settings.Phase1Deadline != null ? DateTime.Parse(settings.Phase1Deadline) : null;
        _tier3Deadline = settings.Phase2Deadline != null ? DateTime.Parse(settings.Phase2Deadline) : null;
        _isPhase1Complete = settings.IsPhase1Complete;
        _isProcessComplete = settings.IsProcessComplete;
        SetupTimer();
    }


    public virtual async Task OnNavigatedToAsync(int supervisorId)
    {
        _currentSupervisorId = supervisorId;
        SelectedSupervisor = await _userClient.GetByIdAsync(supervisorId);
        await LoadSupervisorDataAsync(supervisorId);
        await LoadMatchingSettingsAsync();
    }

    [RelayCommand]
    private async Task ShowPhaseInfo()
    {
        if (_isProcessComplete)
            await _toastService.ShowInfoAsync(_loc.Get("Supervisor_PhaseComplete_Info_Title"), _loc.Get("Supervisor_PhaseComplete_Info_Body"));
        else if (IsPhase1Active)
            await _toastService.ShowInfoAsync(_loc.Get("Supervisor_Phase1Info_Title"), _loc.Get("Supervisor_Phase1Info_Body"));
        else if (IsPhase2Active)
            await _toastService.ShowInfoAsync(_loc.Get("Supervisor_Phase2Info_Title"), _loc.Get("Supervisor_Phase2Info_Body"));
    }

    private void SetupTimer()
    {
        _timerCts?.Cancel();
        _timerCts = new CancellationTokenSource();
        _ = RunTimerAsync(_timerCts.Token);
    }

    private async Task RunTimerAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        UpdatePhaseTimer();
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                UpdatePhaseTimer();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void UpdatePhaseTimer()
    {
        if (_isProcessComplete)
        {
            IsPhase1Active = false;
            IsPhase2Active = false;
            IsPhaseBannerVisible = true;
            BannerTitle = _loc.Get("Supervisor_BannerComplete_Title");
            BannerSubtitle = _loc.Get("Supervisor_BannerComplete_Subtitle");
            BannerTimer = string.Empty;
            return;
        }

        IsPhaseBannerVisible = true;

        if (!_isPhase1Complete)
        {
            IsPhase1Active = true;
            IsPhase2Active = false;
            BannerTitle = _loc.Get("Supervisor_BannerPhase1_Title");
            BannerSubtitle = _loc.Get("Supervisor_BannerPhase1_Subtitle");
            if (_tier1Deadline.HasValue)
            {
                var diff = _tier1Deadline.Value - DateTime.Now;
                BannerTimer = diff.TotalSeconds > 0
                    ? _loc.Format("Supervisor_BannerTimer_RegClosesIn", $"{diff.Days}d {diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s")
                    : _loc.Get("Supervisor_BannerTimer_RegDeadlineReached");
            }
            else BannerTimer = _loc.Get("Supervisor_BannerTimer_NoDeadline");
        }
        else
        {
            IsPhase1Active = false;
            IsPhase2Active = true;
            BannerTitle = _loc.Get("Supervisor_BannerPhase2_Title");
            BannerSubtitle = _loc.Get("Supervisor_BannerPhase2_Subtitle");
            if (_tier3Deadline.HasValue)
            {
                var diff = _tier3Deadline.Value - DateTime.Now;
                BannerTimer = diff.TotalSeconds > 0
                    ? _loc.Format("Supervisor_BannerTimer_AutoMatchIn", $"{diff.Days}d {diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s")
                    : _loc.Get("Supervisor_BannerTimer_SelectionDeadlineReached");
            }
            else BannerTimer = _loc.Get("Supervisor_BannerTimer_NoDeadline");
        }
    }

    private static bool IsStudentInfoFilled(StudentModel s)
    {
        if (s.Grade == null || s.Grade.Id <= 0 || s.ClassNum <= 0) return false;
        if (!s.IsMentor && !s.IsMentee) return false;
        if (s.IsMentor && (s.MentorProfile == null || s.MentorProfile.SubjectToTeach <= 0)) return false;
        if (s.IsMentee && (s.MenteeProfile == null || s.MenteeProfile.SubjectToLearn <= 0)) return false;

        return true;
    }
}
