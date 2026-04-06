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
using System.Windows.Threading;
using System.Windows;
using System;

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

        // Cached so we can reload data after returning from sub-pages
        private int _currentSupervisorId;

        [ObservableProperty] private SupervisorModel? _selectedSupervisor;

        
        [ObservableProperty] private ObservableCollection<Pair> _pairsSupervised = [];
        
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

        // ── Matching-Flow State (Warnings for Tier 5) ─────────────────────────

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
            SettingsService settingsService)
        {
            _navigationService = navigationService;
            _pairService = pairService;
            _issueService = issueService;
            _userService = userService;
            _reviewService = reviewService;
            _settingsService = settingsService;
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

            // Load Nudge Dashboard data
            var allUsers = await _userService.GetAllUsersAsync();
            var myStudents = allUsers.OfType<StudentModel>()
                .Where(s => SelectedSupervisor != null && s.Grade?.Id == SelectedSupervisor.Grade?.Id && s.ClassNum == SelectedSupervisor.ClassNum)
                .ToList();

            int totalStudents = myStudents.Count;
            var inactive = new List<StudentModel>();
            
            foreach (var student in myStudents)
            {
                bool isProfileIncomplete = student.Grade == null || student.Grade.Id == 0 || student.ClassNum <= 0;
                if (student.IsMentor && student.MentorProfile?.SubjectToTeach <= 0) isProfileIncomplete = true;
                if (student.IsMentee && student.MenteeProfile?.SubjectToLearn <= 0) isProfileIncomplete = true;

                if (isProfileIncomplete)
                {
                    inactive.Add(student);
                }
            }

            InactiveStudents = new ObservableCollection<StudentModel>(inactive);
            
            if (totalStudents > 0)
            {
                int registered = totalStudents - inactive.Count;
                ClassProgressPercent = (double)registered / totalStudents * 100;
                ClassProgressInfo = $"Class {SelectedSupervisor?.ClassNum}: {(int)ClassProgressPercent}% Complete ({registered}/{totalStudents} Registered)";
            }
            else
            {
                ClassProgressPercent = 0;
                ClassProgressInfo = "No students in your class.";
            }
        }

        private async Task LoadMatchingSettingsAsync()
        {
            // Timer
            _tier1Deadline = await _settingsService.GetPhase1DeadlineAsync();
            _tier3Deadline = await _settingsService.GetPhase2DeadlineAsync();
            _isPhase1Complete = await _settingsService.GetIsPhase1CompleteAsync();

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
        private void ShowPhaseInfo()
        {
            if (IsPhase1Active)
            {
                MessageBox.Show("Phase 1: Mentee Enrollment.\nYour students are currently selecting mentors. Keep an eye on the inactive students list.", "Phase 1 Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (IsPhase2Active)
            {
                MessageBox.Show("Phase 2: Algorithmic Matching.\nThe system is processing leftover unmatched students. You may need to review fallback assignments soon.", "Phase 2 Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
            // For Supervisor, we almost always show the banner unless all phases are done (for our case we'll just show it always until we decide otherwise, but let's toggle visibility based on if there's any active deadline)
            IsPhaseBannerVisible = true;
            
            if (!_isPhase1Complete)
            {
                IsPhase1Active = true;
                IsPhase2Active = false;
                BannerTitle = "Phase 1: Mentee Matchmaking Window";
                BannerSubtitle = "Mentees are currently browsing and selecting mentors.";
                BannerColor = "#E3F2FD";
                BannerTextColor = "#1565C0";

                if (_tier1Deadline.HasValue)
                {
                    var diff = _tier1Deadline.Value - DateTime.Now;
                    if (diff.TotalSeconds > 0)
                        BannerTimer = $"Ends in: {diff.Days}d {diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s";
                    else
                        BannerTimer = "Deadline Reached (Awaiting system run)";
                }
                else
                {
                    BannerTimer = "Pending System Activation";
                }
            }
            else
            {
                IsPhase1Active = false;
                IsPhase2Active = true;
                BannerTitle = "Phase 2: Algorithmic Fallback";
                BannerSubtitle = "The system is matching remaining mentees automatically.";
                BannerColor = "#F3E5F5";
                BannerTextColor = "#6A1B9A";

                if (_tier3Deadline.HasValue)
                {
                    var diff = _tier3Deadline.Value - DateTime.Now;
                    if (diff.TotalSeconds > 0)
                        BannerTimer = $"Runs in: {diff.Days}d {diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s";
                    else
                        BannerTimer = "Deadline Reached (Awaiting system run)";
                }
                else
                {
                    BannerTimer = "Pending Algorithmic Run";
                }
            }
        }
    }
}
