using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Supervisor
{
    public partial class SupervisorDashboardViewModel : ObservableObject, INavigatable<int>
    {
        protected readonly INavigationService _navigationService;
        protected readonly PairService _pairService;
        protected readonly IssueService _issueService;
        protected readonly UserService _userService;
        protected readonly ReviewService _reviewService;
        protected readonly SettingsService _settingsService;
        protected readonly MatchingFlowService _matchingFlowService;

        // Cached so we can reload data after returning from sub-pages
        private int _currentSupervisorId;

        [ObservableProperty] private SupervisorModel? _selectedSupervisor;

        
        [ObservableProperty] private ObservableCollection<Pair> _pairsSupervised = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
        [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
        [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
        private ObservableCollection<IssueModel> _allIssues = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
        [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
        [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
        private Pair? _issueFilterPair;

        public IEnumerable<IssueModel> FilteredPendingIssues => AllIssues
            .Where(i => !i.IsResolved)
            .Where(i => IssueFilterPair == null || i.ReportedByUserId == IssueFilterPair.Mentor.Id || i.ReportedByUserId == IssueFilterPair.Mentee.Id);

        public IEnumerable<IssueModel> FilteredResolvedIssues => AllIssues
            .Where(i => i.IsResolved)
            .Where(i => IssueFilterPair == null || i.ReportedByUserId == IssueFilterPair.Mentor.Id || i.ReportedByUserId == IssueFilterPair.Mentee.Id);
            
        public int ResolvedIssuesCount => FilteredResolvedIssues.Count();

        [ObservableProperty] private object? _selectedPaneContent;

        [ObservableProperty] private bool _showResolvedIssues;

        [RelayCommand]
        private void ToggleResolvedIssues() => ShowResolvedIssues = !ShowResolvedIssues;

        // ── Matching-Flow State ───────────────────────────────────────────────

        [ObservableProperty] private DateTime? _tier1Deadline;
        [ObservableProperty] private DateTime? _tier3Deadline;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MatchingStatusLabel))]
        private string _matchingPhaseLabel = "Tier 1 – Direct Request Window";

        public string MatchingStatusLabel => _matchingPhaseLabel;

        [ObservableProperty] private string _matchingOperationResult = string.Empty;
        [ObservableProperty] private bool _hasMatchingResult;

        /// <summary>Pairs that were randomly matched due to incomplete profiles (Tier 5 warnings).</summary>
        [ObservableProperty] private ObservableCollection<Pair> _incompleteProfilePairs = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasIncompleteProfileWarnings))]
        private int _incompleteProfileCount;

        public bool HasIncompleteProfileWarnings => IncompleteProfileCount > 0;

        public SupervisorDashboardViewModel(
            INavigationService navigationService, 
            PairService pairService, 
            IssueService issueService, 
            UserService userService,
            ReviewService reviewService,
            SettingsService settingsService,
            MatchingFlowService matchingFlowService)
        {
            _navigationService = navigationService;
            _pairService = pairService;
            _issueService = issueService;
            _userService = userService;
            _reviewService = reviewService;
            _settingsService = settingsService;
            _matchingFlowService = matchingFlowService;
        }

        [RelayCommand]
        private async Task SelectIssue(IssueModel? issue)
        {
            if (issue != null)
            {
                var vm = new IssueViewModel(_navigationService, _issueService);

                var pair = PairsSupervised.FirstOrDefault(p => p.Mentor.Id == issue.ReportedByUserId || p.Mentee.Id == issue.ReportedByUserId);
                if (pair != null)
                {
                    vm.RelatedPairName = $"Originating from Pair: {pair.Mentor.UserName} & {pair.Mentee.UserName}";
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
        private async Task SelectPair(Pair? pair)
        {
            IssueFilterPair = pair;

            if (pair != null)
            {
                var vm = new PairDetailsViewModel(_pairService, _issueService, _reviewService, _settingsService)
                {
                    ShowIssues = false
                };
                await vm.OnNavigatedToAsync(pair.Id);
                SelectedPaneContent = vm;
            }
            else 
            {
                 SelectedPaneContent = null;
            }
        }

        // ── Deadline Management ───────────────────────────────────────────────

        [RelayCommand]
        private async Task SaveTier1Deadline()
        {
            if (Tier1Deadline.HasValue)
            {
                await _settingsService.SetTier1DeadlineAsync(Tier1Deadline.Value);
                UpdateMatchingPhase();
                MatchingOperationResult = "✓ Tier 1 deadline saved.";
                HasMatchingResult = true;
            }
        }

        [RelayCommand]
        private async Task SaveTier3Deadline()
        {
            if (Tier3Deadline.HasValue)
            {
                await _settingsService.SetTier3DeadlineAsync(Tier3Deadline.Value);
                UpdateMatchingPhase();
                MatchingOperationResult = "✓ Tier 3 deadline saved.";
                HasMatchingResult = true;
            }
        }

        private void UpdateMatchingPhase()
        {
            var now = DateTime.Now;
            if (Tier1Deadline == null)
                MatchingPhaseLabel = "Tier 1 – Direct Request Window (no deadline set)";
            else if (now < Tier1Deadline)
                MatchingPhaseLabel = $"Tier 1 – Direct Request Window (closes {Tier1Deadline:d})";
            else if (Tier3Deadline == null || now < Tier3Deadline)
                MatchingPhaseLabel = $"Tier 3 – Selection Gallery (closes {Tier3Deadline?.ToString("d") ?? "no deadline set"})";
            else
                MatchingPhaseLabel = "Tier 4/5 – Auto-Match phase";
        }

        // ── Algorithmic Triggers ──────────────────────────────────────────────

        [RelayCommand]
        private async Task RunTier2Matrix()
        {
            MatchingOperationResult = "Running Tier 2: Generating score matrix…";
            HasMatchingResult = true;

            var result = await _matchingFlowService.GenerateScoreMatrixAsync();

            MatchingOperationResult = result.Success
                ? "✓ Score matrix generated. Tier 3 gallery is now active for mentees."
                : $"✗ {result.ErrorMessage}";
        }

        [RelayCommand]
        private async Task RunTier4AutoMatch()
        {
            MatchingOperationResult = "Running Tier 4: Algorithmic auto-match…";
            HasMatchingResult = true;

            var result = await _matchingFlowService.RunAutoMatchAsync(_currentSupervisorId);

            MatchingOperationResult = result.Success
                ? $"✓ Auto-match complete. {result.Data} new pairs created."
                : $"✗ {result.ErrorMessage}";

            await LoadSupervisorDataAsync(_currentSupervisorId);
        }

        [RelayCommand]
        private async Task RunTier5Fallback()
        {
            MatchingOperationResult = "Running Tier 5: Fallback random assignment…";
            HasMatchingResult = true;

            var result = await _matchingFlowService.RunFallbackMatchAsync(_currentSupervisorId);

            MatchingOperationResult = result.Success
                ? $"✓ Fallback complete. {result.Data} pairs randomly assigned. Check warnings below."
                : $"✗ {result.ErrorMessage}";

            await LoadSupervisorDataAsync(_currentSupervisorId);
        }

        // ── Data Loading ──────────────────────────────────────────────────────

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            var pairsResult = await _pairService.GetPairsBySupervisorAsync(supervisorId);
            var issuesResult = await _issueService.GetIssuesBySupervisorAsync(supervisorId);

            if (pairsResult.Success)
            {
                PairsSupervised = new ObservableCollection<Pair>(pairsResult.Data ?? []);

                var warnings = PairsSupervised
                    .Where(p => p.IsProfileIncomplete || p.MatchTier == MatchTier.FallbackRandom)
                    .ToList();
                IncompleteProfilePairs = new ObservableCollection<Pair>(warnings);
                IncompleteProfileCount = warnings.Count;
            }

            if (issuesResult.Success)
            {
                AllIssues = new ObservableCollection<IssueModel>(issuesResult.Data ?? []);
            }
        }

        private async Task LoadMatchingSettingsAsync()
        {
            Tier1Deadline = await _settingsService.GetTier1DeadlineAsync();
            Tier3Deadline = await _settingsService.GetTier3DeadlineAsync();
            UpdateMatchingPhase();
        }

        public new async Task OnNavigatedToAsync()
        {
            await LoadSupervisorDataAsync(_currentSupervisorId);
        }

        public virtual async Task OnNavigatedToAsync(int supervisorId)
        {
            _currentSupervisorId = supervisorId;
            await LoadSupervisorDataAsync(supervisorId);
            await LoadMatchingSettingsAsync();
            Result<UserModel> res = await _userService.GetUserByIdAsync(supervisorId);
            SelectedSupervisor = res.Data as SupervisorModel;
        }
    }
}
