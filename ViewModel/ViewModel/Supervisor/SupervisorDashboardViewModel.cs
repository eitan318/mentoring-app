using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.Admin;
using MentoringApp.ViewModel.ViewModel.User;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using System;

namespace MentoringApp.ViewModel.ViewModel.Supervisor
{
    /// <summary>
    /// Supervisor dashboard ViewModel. Loaded with the supervisor's ID via
    /// <see cref="OnNavigatedToAsync(int)"/> so it can be navigated to either from
    /// the supervisor's own login or from the admin's "Inspect" action.
    /// Provides:
    ///   - Pair list with optional pair-detail side-pane (<see cref="SelectedPaneContent"/>).
    ///   - Issue list filterable by selected pair, with pending/resolved split.
    ///   - Class completion gauge (students with complete profiles vs. total).
    ///   - Phase banner with live countdown driven by a DispatcherTimer.
    ///   - Tier-5 warnings: pairs flagged as profile-incomplete or FallbackRandom.
    /// </summary>
    public partial class SupervisorDashboardViewModel : ObservableObject, INavigatable<int>
    {
        protected readonly INavigationService _navigationService;
        protected readonly PairService _pairService;
        protected readonly IssueService _issueService;
        protected readonly UserService _userService;
        protected readonly ReviewService _reviewService;
        protected readonly SettingsService _settingsService;
        private readonly UserStore _userStore;
        private readonly IToastService _toastService;
        private readonly ILocalizationService _loc;

        // Cached so we can reload data after returning from sub-pages
        private int _currentSupervisorId;

        [ObservableProperty] private SupervisorModel? _selectedSupervisor;

        
        [ObservableProperty] private ObservableCollection<PairProgressItem> _pairsSupervised = [];
        
        [ObservableProperty] private ObservableCollection<StudentModel> _inactiveStudents = [];
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
        private ObservableCollection<IssueModel> _allIssues = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
        [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
        [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
        [NotifyPropertyChangedFor(nameof(IssuesSectionContextTitle))]
        private Pair? _issueFilterPair;

        public string IssuesSectionContextTitle => IssueFilterPair == null
            ? "Issues"
            : $"Issues of {IssueFilterPair.Mentor.UserName} & {IssueFilterPair.Mentee.UserName}";

        /// <summary>
        /// Pending issues, optionally scoped to the selected pair's participants.
        /// When <see cref="IssueFilterPair"/> is null, all supervised issues are shown.
        /// </summary>
        public IEnumerable<IssueModel> FilteredPendingIssues => AllIssues
            .Where(i => !i.IsResolved)
            .Where(i => IssueFilterPair == null || i.ReportedByUserId == IssueFilterPair.Mentor.Id || i.ReportedByUserId == IssueFilterPair.Mentee.Id);

        /// <summary>Resolved issues scoped by the same pair filter.</summary>
        public IEnumerable<IssueModel> FilteredResolvedIssues => AllIssues
            .Where(i => i.IsResolved)
            .Where(i => IssueFilterPair == null || i.ReportedByUserId == IssueFilterPair.Mentor.Id || i.ReportedByUserId == IssueFilterPair.Mentee.Id);
            
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

        // ── Matching-Flow State (Warnings for Tier 5) ─────────────────────────

        /// <summary>Pairs that were randomly matched due to incomplete profiles (Tier 5 warnings).</summary>
        [ObservableProperty] private ObservableCollection<Pair> _incompleteProfilePairs = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasIncompleteProfileWarnings))]
        private int _incompleteProfileCount;

        public bool HasIncompleteProfileWarnings => IncompleteProfileCount > 0;

        /// <summary>True when the current logged-in user is an admin (viewing someone else's dashboard).</summary>
        public bool IsAdminViewing => _userStore.User is AdminModel;

        public SupervisorDashboardViewModel(
            INavigationService navigationService,
            PairService pairService,
            IssueService issueService,
            UserService userService,
            ReviewService reviewService,
            SettingsService settingsService,
            UserStore userStore,
            IToastService toastService,
            ILocalizationService loc)
        {
            _navigationService = navigationService;
            _pairService = pairService;
            _issueService = issueService;
            _userService = userService;
            _reviewService = reviewService;
            _settingsService = settingsService;
            _userStore = userStore;
            _toastService = toastService;
            _loc = loc;
        }

        [RelayCommand]
        private async Task SelectIssue(IssueModel? issue)
        {
            if (issue != null)
            {
                var vm = new IssueViewModel(_navigationService, _issueService);

                var item = PairsSupervised.FirstOrDefault(p => p.Mentor.Id == issue.ReportedByUserId || p.Mentee.Id == issue.ReportedByUserId);
                if (item != null)
                {
                    vm.RelatedPairName = _loc.Format("Supervisor_RelatedPairName_Format", item.Mentor.UserName, item.Mentee.UserName);
                }

                vm.OnCloseRequested = () => SelectedPaneContent = null;
                vm.OnIssueResolved = () => 
                {
                    SelectedPaneContent = null;
                    _ = LoadSupervisorDataAsync(_currentSupervisorId);
                };
                await vm.OnNavigatedToAsync(issue.Id);
                SelectedPaneContent = vm;
            }
        }

        [RelayCommand]
        private async Task SelectPair(PairProgressItem? item)
        {
            IssueFilterPair = item?.Pair;

            if (item != null)
            {
                var vm = new PairDetailsViewModel(_pairService, _issueService, _reviewService, _settingsService)
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

        // ── Data Loading ──────────────────────────────────────────────────────

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            var pairsResult = await _pairService.GetPairsBySupervisorAsync(supervisorId);
            var issuesResult = await _issueService.GetIssuesBySupervisorAsync(supervisorId);

            if (pairsResult.Success)
            {
                var pairs = pairsResult.Data ?? [];
                double requiredHours = await _settingsService.GetMeetingHoursBarrierAsync();

                var reviewTasks = pairs.Select(pair =>
                    _reviewService.GetReviewsByPairAsync(pair.Id).ContinueWith(t =>
                        (pair, hours: t.Result.Success ? t.Result.Data?.Sum(r => r.AmountOfHours) ?? 0 : 0)));

                var reviewResults = await Task.WhenAll(reviewTasks);
                var progressItems = reviewResults
                    .Select(r => new PairProgressItem(r.pair, r.hours, requiredHours))
                    .ToList();

                PairsSupervised = new ObservableCollection<PairProgressItem>(
                    progressItems.OrderBy(p => p.TotalMeetingHours));

                var warnings = progressItems
                    .Where(p => p.IsProfileIncomplete || p.MatchTier == MatchTier.FallbackRandom)
                    .Select(p => p.Pair)
                    .ToList();
                IncompleteProfilePairs = new ObservableCollection<Pair>(warnings);
                IncompleteProfileCount = warnings.Count;
            }

            if (issuesResult.Success)
            {
                AllIssues = new ObservableCollection<IssueModel>(issuesResult.Data ?? []);
            }

            // Build the class-completion gauge across ALL classes assigned to this supervisor.
            var allUsers = await _userService.GetAllUsersAsync();

            var assignedSlots = SelectedSupervisor?.AssignedClasses
                .Select(c => (gradeId: c.Grade.Id, classNum: c.ClassNum))
                .ToHashSet() ?? [];

            var myStudents = allUsers.OfType<StudentModel>()
                .Where(s => s.Grade != null && assignedSlots.Contains((s.Grade.Id, s.ClassNum)))
                .ToList();

            int totalStudents = myStudents.Count;
            var inactive = myStudents.Where(s => !AdminDashboardViewModel.IsStudentInfoFilled(s)).ToList();

            InactiveStudents = new ObservableCollection<StudentModel>(inactive);

            if (totalStudents > 0)
            {
                int registered = totalStudents - inactive.Count;
                ClassProgressPercent = (double)registered / totalStudents * 100;
                string classLabel = assignedSlots.Count == 1
                    ? $"Class {assignedSlots.First().classNum}"
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
            _tier1Deadline    = await _settingsService.GetPhase1DeadlineAsync();
            _tier3Deadline    = await _settingsService.GetPhase2DeadlineAsync();
            _isPhase1Complete = await _settingsService.GetIsPhase1CompleteAsync();
            _isProcessComplete = await _settingsService.GetIsProcessCompleteAsync();

            SetupTimer();
        }

        public new async Task OnNavigatedToAsync()
        {
            await LoadSupervisorDataAsync(_currentSupervisorId);
        }

        public virtual async Task OnNavigatedToAsync(int supervisorId)
        {
            _currentSupervisorId = supervisorId;
            Result<UserModel> res = await _userService.GetUserByIdAsync(supervisorId);
            SelectedSupervisor = res.Data as SupervisorModel;

            await LoadSupervisorDataAsync(supervisorId);
            await LoadMatchingSettingsAsync();
        }

        [RelayCommand]
        private async Task ShowPhaseInfo()
        {
            if (_isProcessComplete)
            {
                await _toastService.ShowInfoAsync(
                    _loc.Get("Supervisor_PhaseComplete_Info_Title"),
                    _loc.Get("Supervisor_PhaseComplete_Info_Body"));
            }
            else if (IsPhase1Active)
            {
                await _toastService.ShowInfoAsync(
                    _loc.Get("Supervisor_Phase1Info_Title"),
                    _loc.Get("Supervisor_Phase1Info_Body"));
            }
            else if (IsPhase2Active)
            {
                await _toastService.ShowInfoAsync(
                    _loc.Get("Supervisor_Phase2Info_Title"),
                    _loc.Get("Supervisor_Phase2Info_Body"));
            }
        }

        private void SetupTimer()
        {
            _timer?.Stop();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdatePhaseTimer();
            UpdatePhaseTimer(); // Initial call
            _timer.Start();
        }

        private void UpdatePhaseTimer()
        {
            // ── Phase 3: Matching complete ────────────────────────────────────
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

            // ── Phase 1: Registration open ────────────────────────────────────
            // Admin has not yet started the selection phase.
            // Students are signing up, filling profiles, and mentees may send
            // direct pairing requests to mentors.
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
                else
                {
                    BannerTimer = _loc.Get("Supervisor_BannerTimer_NoDeadline");
                }
            }
            // ── Phase 2: Mentor selection open ────────────────────────────────
            // Admin started the selection phase.
            // Each unmatched mentee can now pick from their top algorithmically
            // ranked mentor suggestions. The auto-match runs at the deadline.
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
                else
                {
                    BannerTimer = _loc.Get("Supervisor_BannerTimer_NoDeadline");
                }
            }
        }
    }
}
