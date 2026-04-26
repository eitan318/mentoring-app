using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.ViewModel.Helpers;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.User;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.Windows.Threading;

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

    [ObservableProperty] private UserResponse? _selectedSupervisor;
    [ObservableProperty] private ObservableCollection<PairProgressItem> _pairsSupervised = [];
    [ObservableProperty] private ObservableCollection<UserResponse> _inactiveStudents = [];
    [ObservableProperty] private string _classProgressInfo = string.Empty;
    [ObservableProperty] private double _classProgressPercent = 0.0;

    [ObservableProperty] private bool _isPhaseBannerVisible;
    [ObservableProperty] private string _bannerTitle = string.Empty;
    [ObservableProperty] private string _bannerSubtitle = string.Empty;
    [ObservableProperty] private string _bannerTimer = string.Empty;
    [ObservableProperty] private string _bannerColor = "#E3F2FD";
    [ObservableProperty] private string _bannerTextColor = "#1565C0";
    [ObservableProperty] private bool _isPhase1Active;
    [ObservableProperty] private bool _isPhase2Active;

    private DispatcherTimer? _timer;
    private DateTime? _tier1Deadline;
    private DateTime? _tier3Deadline;
    private bool _isPhase1Complete;
    private bool _isProcessComplete;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
    [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
    [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
    private ObservableCollection<IssueResponse> _allIssues = [];

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
        : $"Issues of {IssueFilterPair.MentorName} & {IssueFilterPair.MenteeName}";

    partial void OnIssueFilterPairChanged(PairProgressItem? value)
    {
        _issueFilterMentorId = value?.MentorId;
        _issueFilterMenteeId = value?.MenteeId;
    }

    public IEnumerable<IssueResponse> FilteredPendingIssues => AllIssues
        .Where(i => !i.IsResolvedBool)
        .Where(i => IssueFilterPair == null ||
            i.ReportedByUserId == _issueFilterMentorId ||
            i.ReportedByUserId == _issueFilterMenteeId);

    public IEnumerable<IssueResponse> FilteredResolvedIssues => AllIssues
        .Where(i => i.IsResolvedBool)
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

    [RelayCommand]
    private async Task SelectIssue(IssueResponse? issue)
    {
        if (issue != null)
        {
            var vm = new IssueViewModel(_navigationService, _issueClient);
            var item = PairsSupervised.FirstOrDefault(p =>
                p.MentorId == issue.ReportedByUserId || p.MenteeId == issue.ReportedByUserId);
            if (item != null)
                vm.RelatedPairName = _loc.Format("Supervisor_RelatedPairName_Format", item.MentorName, item.MenteeName);
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

    private async Task LoadSupervisorDataAsync(int supervisorId)
    {
        var pairs = await _pairClient.GetBySupervisorAsync(supervisorId);
        var issues = await _issueClient.GetBySupervisorAsync(supervisorId);
        var settings = await _settingsClient.GetAllAsync();

        var allUsers = await _userClient.GetAllAsync();
        var userMap = allUsers.ToDictionary(u => u.Id, u => u.UserName);

        double requiredHours = settings.MeetingHoursBarrier;

        var reviewTasks = pairs.Select(pair =>
            _reviewClient.GetByPairAsync(pair.Id).ContinueWith(t =>
                (pair, hours: t.Result.Sum(r => r.AmountOfHours))));

        var reviewResults = await Task.WhenAll(reviewTasks);
        var progressItems = reviewResults
            .Select(r => new PairProgressItem(
                r.pair,
                userMap.TryGetValue(r.pair.MentorId, out var mn) ? mn : $"User {r.pair.MentorId}",
                userMap.TryGetValue(r.pair.MenteeId, out var men) ? men : $"User {r.pair.MenteeId}",
                r.hours,
                requiredHours))
            .ToList();

        PairsSupervised = new ObservableCollection<PairProgressItem>(
            progressItems.OrderBy(p => p.TotalMeetingHours));

        var warnings = progressItems
            .Where(p => p.IsProfileIncomplete || p.MatchTier == MatchTier.FallbackRandom)
            .ToList();
        IncompleteProfilePairs = new ObservableCollection<PairProgressItem>(warnings);
        IncompleteProfileCount = warnings.Count;

        AllIssues = new ObservableCollection<IssueResponse>(issues);

        // Class completion gauge
        var supervisorClasses = await _referenceClient.GetSchoolClassesBySupervisorAsync(supervisorId);
        var assignedSlots = supervisorClasses.Select(c => (c.GradeId, c.ClassNum)).ToHashSet();

        var myStudents = allUsers
            .Where(u => u.IsStudent && u.GradeId.HasValue &&
                assignedSlots.Contains((u.GradeId.Value, u.ClassNum ?? 0)))
            .ToList();

        int totalStudents = myStudents.Count;
        var inactive = myStudents.Where(s => !IsStudentInfoFilled(s)).ToList();

        InactiveStudents = new ObservableCollection<UserResponse>(inactive);

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

    private async Task LoadMatchingSettingsAsync()
    {
        var settings = await _settingsClient.GetAllAsync();
        _tier1Deadline = settings.Phase1Deadline != null ? DateTime.Parse(settings.Phase1Deadline) : null;
        _tier3Deadline = settings.Phase2Deadline != null ? DateTime.Parse(settings.Phase2Deadline) : null;
        _isPhase1Complete = settings.IsPhase1Complete;
        _isProcessComplete = settings.IsProcessComplete;
        SetupTimer();
    }

    public new async Task OnNavigatedToAsync()
    {
        await LoadSupervisorDataAsync(_currentSupervisorId);
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
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) => UpdatePhaseTimer();
        UpdatePhaseTimer();
        _timer.Start();
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

    private static bool IsStudentInfoFilled(UserResponse s)
    {
        if (!s.GradeId.HasValue || s.GradeId == 0 || (s.ClassNum ?? 0) <= 0) return false;
        if (!s.IsMentor && !s.IsMentee) return false;
        if (s.IsMentor && !s.MentorSubjectId.HasValue) return false;
        if (s.IsMentee && !s.MenteeSubjectId.HasValue) return false;
        return true;
    }
}
