using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MentoringApp.ViewModel.ViewModel.Student
{
    /// <summary>
    /// Student dashboard ViewModel. Dynamically composes the tab list (<see cref="Pairs"/>)
    /// based on the current phase and the student's match status:
    /// <list type="bullet">
    ///   <item>Always shown when matched: <see cref="MentorDashboardViewModel"/> or <see cref="MenteeDashboardViewModel"/>.</item>
    ///   <item>Phase 1 unmatched mentee: <see cref="BrowseMentorsViewModel"/> (browse &amp; request).</item>
    ///   <item>Phase 2 unmatched mentee: <see cref="SelectionGalleryViewModel"/> (top-3 picks).</item>
    ///   <item>Phase 2 mentor: <see cref="MentorRequestsViewModel"/> (review incoming requests).</item>
    /// </list>
    /// The <see cref="Pairs"/> collection uses <c>object</c> so the tab control's DataTemplate
    /// selector can dispatch to different templates for each ViewModel type.
    /// </summary>
    public partial class StudentDashboardViewModel : ObservableObject, INavigatable
    {
        // Heterogeneous tab items — WPF resolves the correct DataTemplate per item type.
        public ObservableCollection<object> Pairs { get; set; } = new();

        public bool HasNoPairs => Pairs.Count == 0;

        [ObservableProperty]
        private object? _selectedPair;

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

        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;
        private readonly IToastService _toastService;
        private readonly ILocalizationService _loc;
        private readonly UserStore _userStore;
        private readonly PairService _pairService;
        private readonly IssueService _issueService;
        private readonly ReviewService _reviewService;
        private readonly SettingsService _settingsService;
        private readonly BrowseMentorsViewModel _browseMentorsVm;
        private readonly SelectionGalleryViewModel _selectionGalleryVm;
        private readonly MentorRequestsViewModel _mentorRequestsVm;

        public StudentDashboardViewModel(
            INavigationService navigationService,
            IWindowService windowService,
            IToastService toastService,
            ILocalizationService loc,
            UserStore userStore,
            PairService pairService,
            IssueService issueService,
            ReviewService reviewService,
            SettingsService settingsService,
            BrowseMentorsViewModel browseMentorsVm,
            SelectionGalleryViewModel selectionGalleryVm,
            MentorRequestsViewModel mentorRequestsVm)
        {
            _windowService = windowService;
            _toastService = toastService;
            _loc = loc;
            _navigationService = navigationService;
            _userStore = userStore;
            _pairService = pairService;
            _issueService = issueService;
            _reviewService = reviewService;
            _settingsService = settingsService;
            _browseMentorsVm = browseMentorsVm;
            _selectionGalleryVm = selectionGalleryVm;
            _mentorRequestsVm = mentorRequestsVm;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            Pairs.Clear();
            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) return;

            _tier1Deadline = await _settingsService.GetPhase1DeadlineAsync();
            _tier3Deadline = await _settingsService.GetPhase2DeadlineAsync();
            _isPhase1Complete = await _settingsService.GetIsPhase1CompleteAsync();

            SetupTimer();

            // Track per-role match status independently so dual-role students
            // always get the correct set of tabs regardless of the other role.
            bool mentorIsMatched = false;
            bool menteeIsMatched = false;

            // --- Real pair tabs (always shown when matched) ---
            if (currentUser.IsMentor)
            {
                var result = await _pairService.GetPairByMentorAsync(currentUser.Id);
                if (result.Success && result.Data != null)
                {
                    var vm = new MentorDashboardViewModel(
                        _windowService, _navigationService, _issueService,
                        _reviewService, _userStore, _settingsService, result.Data);
                    await vm.LoadDataAsync();
                    Pairs.Add(vm);
                    mentorIsMatched = true;
                }
            }

            if (currentUser.IsMentee)
            {
                var result = await _pairService.GetPairByMenteeAsync(currentUser.Id);
                if (result.Success && result.Data != null)
                {
                    var vm = new MenteeDashboardViewModel(
                        _windowService, _navigationService, _issueService,
                        _reviewService, _userStore, _settingsService, result.Data);
                    await vm.LoadDataAsync();
                    Pairs.Add(vm);
                    menteeIsMatched = true;
                }
            }

            // --- Phase 2 tabs for each unmatched role (phase 1 is over) ---
            if (_isPhase1Complete)
            {
                if (currentUser.IsMentee && !menteeIsMatched)
                {
                    await _selectionGalleryVm.LoadAsync();
                    Pairs.Add(_selectionGalleryVm);
                }

                if (currentUser.IsMentor && !mentorIsMatched)
                {
                    // Mentor is passive in Phase 2: mentees choose, no accept/decline
                    _mentorRequestsVm.IsPhase2Active = true;
                    await _mentorRequestsVm.LoadAsync();
                    Pairs.Add(_mentorRequestsVm);
                }
            }

            // --- Phase 1 tabs for each unmatched role ---
            if (!_isPhase1Complete)
            {
                if (currentUser.IsMentee && !menteeIsMatched)
                {
                    await _browseMentorsVm.LoadAsync();
                    Pairs.Add(_browseMentorsVm);
                }

                if (currentUser.IsMentor && !mentorIsMatched)
                {
                    _mentorRequestsVm.IsPhase2Active = false;
                    await _mentorRequestsVm.LoadAsync();
                    Pairs.Add(_mentorRequestsVm);
                }
            }

            SelectedPair = Pairs.Count > 0 ? Pairs[0] : null;
            OnPropertyChanged(nameof(HasNoPairs));
        }

        [RelayCommand]
        private async Task ShowPhaseInfo()
        {
            if (IsPhase1Active)
                await _toastService.ShowInfoAsync(
                    _loc.Get("Student_Phase1Info_Title"),
                    _loc.Get("Student_Phase1Info_Body"));
            else if (IsPhase2Active)
                await _toastService.ShowInfoAsync(
                    _loc.Get("Student_Phase2Info_Title"),
                    _loc.Get("Student_Phase2Info_Body"));
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
            // Show banner whenever there is an active phase tab (unmatched role).
            // Fully-paired students have no phase tabs, so they see no banner.
            bool hasPhaseTab = Pairs.OfType<BrowseMentorsViewModel>().Any()
                            || Pairs.OfType<MentorRequestsViewModel>().Any()
                            || Pairs.OfType<SelectionGalleryViewModel>().Any();
            if (!hasPhaseTab)
            {
                IsPhaseBannerVisible = false;
                return;
            }

            IsPhaseBannerVisible = true;

            if (!_isPhase1Complete)
            {
                IsPhase1Active = true;
                IsPhase2Active = false;
                BannerTitle = _loc.Get("Student_BannerPhase1_Title");
                BannerSubtitle = _loc.Get("Student_BannerPhase1_Subtitle");
                BannerColor = "#E3F2FD";
                BannerTextColor = "#1565C0";

                if (_tier1Deadline.HasValue)
                {
                    var diff = _tier1Deadline.Value - DateTime.Now;
                    BannerTimer = diff.TotalSeconds > 0
                        ? _loc.Format("Student_BannerTimer_EndsIn", $"{diff.Days}d {diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s")
                        : _loc.Get("Student_BannerTimer_DeadlineReached");
                }
                else
                {
                    BannerTimer = _loc.Get("Student_BannerTimer_PendingActivation");
                }
            }
            else
            {
                IsPhase1Active = false;
                IsPhase2Active = true;
                BannerTitle = _loc.Get("Student_BannerPhase2_Title");
                BannerSubtitle = _loc.Get("Student_BannerPhase2_Subtitle");
                BannerColor = "#F3E5F5";
                BannerTextColor = "#6A1B9A";

                if (_tier3Deadline.HasValue)
                {
                    var diff = _tier3Deadline.Value - DateTime.Now;
                    BannerTimer = diff.TotalSeconds > 0
                        ? _loc.Format("Student_BannerTimer_RunsIn", $"{diff.Days}d {diff.Hours:D2}h {diff.Minutes:D2}m {diff.Seconds:D2}s")
                        : _loc.Get("Student_BannerTimer_DeadlineReached");
                }
                else
                {
                    BannerTimer = _loc.Get("Student_BannerTimer_PendingAlgorithm");
                }
            }
        }
    }


    // ─── Pair dashboard base ───────────────────────────────────────────────────

    /// <summary>
    /// Shared base for the mentor-side and mentee-side tab ViewModels.
    /// Loads issues, reviews, and meeting-hour progress for the current pair.
    /// Concrete subclasses expose role-specific labels and commands.
    /// </summary>
    public abstract partial class PairMemberDashboardViewModel : ObservableObject
    {
        /// <summary>Human-readable label for the counterpart's role, e.g. "MENTOR" or "MENTEE".</summary>
        public abstract string CounterpartRole { get; }

        protected readonly IWindowService _windowService;
        protected readonly INavigationService _navigationService;
        protected readonly IssueService _issueService;
        protected readonly ReviewService _reviewService;
        protected readonly UserStore _userStore;
        protected readonly SettingsService _settingsService;

        [ObservableProperty] private StudentModel _counterpart;
        [ObservableProperty] private SupervisorModel? _supervisor;
        public bool HasSupervisor => Supervisor != null;

        public Pair Pair { get; }

        public ObservableCollection<IssueModel> MyIssues { get; set; } = new();
        public ObservableCollection<Review> RecentReviews { get; set; } = new();

        [ObservableProperty] private double _totalMeetingHours;
        [ObservableProperty] private double _requiredMeetingHours = 10;
        [ObservableProperty] private double _hoursProgress;

        protected PairMemberDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueService issueService,
            ReviewService reviewService,
            UserStore userStore,
            SettingsService settingsService,
            Pair pair,
            StudentModel counterpart)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            _issueService = issueService;
            _reviewService = reviewService;
            _userStore = userStore;
            _settingsService = settingsService;
            Pair = pair;
            Counterpart = counterpart;
            Supervisor = pair.Supervisor;
        }

        public virtual async Task LoadDataAsync()
        {
            var issuesResult = await _issueService.GetIssuesByUserAsync(_userStore.User!.Id);
            if (issuesResult.Success && issuesResult.Data != null)
                MyIssues = new ObservableCollection<IssueModel>(issuesResult.Data);

            var reviewsResult = await _reviewService.GetReviewsByPairAsync(Pair.Id);
            if (reviewsResult.Success && reviewsResult.Data != null)
                RecentReviews = new ObservableCollection<Review>(
                    reviewsResult.Data.OrderByDescending(r => r.Date));

            RequiredMeetingHours = await _settingsService.GetMeetingHoursBarrierAsync();
            TotalMeetingHours = RecentReviews.Sum(r => r.AmountOfHours);
            HoursProgress = RequiredMeetingHours > 0
                ? Math.Min(100, (TotalMeetingHours / RequiredMeetingHours) * 100)
                : 0;

            OnPropertyChanged(nameof(MyIssues));
            OnPropertyChanged(nameof(RecentReviews));
        }

        [RelayCommand]
        private async Task IssueToSupervisor()
        {
            var categoriesResult = await _issueService.GetCategoriesAsync();
            if (categoriesResult.Success && categoriesResult.Data != null)
                await _navigationService.NavigateToAsync<AddIssueViewModel, IEnumerable<IssueCategoryModel>>(categoriesResult.Data);
            else
                await _navigationService.NavigateToAsync<AddIssueViewModel>();
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            await _navigationService.NavigateToAsync<OtherProfileViewModel, int>(Counterpart.Id);
        }
    }

    public partial class MenteeDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTOR";

        [ObservableProperty] private string _mentorSubject;

        public MenteeDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueService issueService,
            ReviewService reviewService,
            UserStore userStore,
            SettingsService settingsService,
            Pair pair)
            : base(windowService, navigationService, issueService, reviewService,
                   userStore, settingsService, pair, pair.Mentor)
        {
            MentorSubject = "Assigned Mentor";
        }
    }

    public partial class MentorDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTEE";

        [ObservableProperty] private double _menteeProgress;

        public MentorDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueService issueService,
            ReviewService reviewService,
            UserStore userStore,
            SettingsService settingsService,
            Pair pair)
            : base(windowService, navigationService, issueService, reviewService,
                   userStore, settingsService, pair, pair.Mentee)
        {
            MenteeProgress = 50.0;
        }

        [RelayCommand]
        private async Task CreateReview()
        {
            await _navigationService.NavigateToAsync<AddReviewViewModel, Pair>(Pair);
            await LoadDataAsync();
        }
    }


    // ─── SelectionGalleryViewModel (Phase 2 mentee tab) ───────────────────────

    public partial class SelectionGalleryViewModel : ObservableObject, INavigatable
    {
        private readonly ILocalizationService _loc;
        public string TabLabel => _loc.Get("Student_TabLabel_TopMatches");

        private readonly MatchingFlowService _matchingFlowService;
        private readonly UserStore _userStore;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private ObservableCollection<MatchScore> _recommendations = [];
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasStatusMessage;
        [ObservableProperty] private bool _alreadyMatched;

        public SelectionGalleryViewModel(
            MatchingFlowService matchingFlowService,
            UserStore userStore,
            SettingsService settingsService,
            ILocalizationService loc)
        {
            _matchingFlowService = matchingFlowService;
            _userStore = userStore;
            _settingsService = settingsService;
            _loc = loc;
        }

        public async Task OnNavigatedToAsync() => await LoadAsync();

        public async Task LoadAsync()
        {
            IsLoading = true;
            Recommendations.Clear();

            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) { IsLoading = false; return; }

            var topRecs = await _matchingFlowService.GetTopRecommendationsAsync(currentUser.Id, topN: 3);
            foreach (var rec in topRecs)
                Recommendations.Add(rec);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task ChooseMentor(MatchScore recommendation)
        {
            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) return;

            int supervisorId = 1;
            var result = await _matchingFlowService.GalleryPickAsync(currentUser.Id, recommendation.MentorId, supervisorId);

            if (result.Success)
            {
                AlreadyMatched = true;
                StatusMessage = _loc.Format("Student_MatchedWith_Message", recommendation.MentorName);
            }
            else
            {
                StatusMessage = $"✗ {result.ErrorMessage}";
            }
            HasStatusMessage = true;
        }
    }


    // ─── MentorRequestsViewModel (Phase 2 mentor tab) ─────────────────────────

    public partial class MentorRequestsViewModel : ObservableObject, INavigatable
    {
        private readonly ILocalizationService _loc;
        public string TabLabel => _loc.Get("Student_TabLabel_MentoringRequests");

        private readonly MatchingFlowService _matchingFlowService;
        private readonly UserStore _userStore;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private ObservableCollection<PairRequest> _pendingRequests = [];
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasStatusMessage;
        [ObservableProperty] private string _requestWindowTimerDisplay = string.Empty;
        /// <summary>True when this tab is shown in Phase 2 — hides Accept/Decline, shows passive info banner.</summary>
        [ObservableProperty] private bool _isPhase2Active;

        public int AssignedSupervisorId { get; set; } = 1;

        public MentorRequestsViewModel(
            MatchingFlowService matchingFlowService,
            UserStore userStore,
            SettingsService settingsService,
            ILocalizationService loc)
        {
            _matchingFlowService = matchingFlowService;
            _userStore = userStore;
            _settingsService = settingsService;
            _loc = loc;
        }

        public async Task OnNavigatedToAsync() => await LoadAsync();

        public async Task LoadAsync()
        {
            IsLoading = true;
            PendingRequests.Clear();

            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null || !currentUser.IsMentor) { IsLoading = false; return; }

            var requests = await _matchingFlowService.GetPendingRequestsForMentorAsync(currentUser.Id);
            foreach (var req in requests)
                PendingRequests.Add(req);

            var tier1Deadline = await _settingsService.GetPhase1DeadlineAsync();
            if (tier1Deadline.HasValue)
            {
                var diff = tier1Deadline.Value - DateTime.Now;
                RequestWindowTimerDisplay = diff.TotalSeconds > 0
                    ? _loc.Format("Student_RequestWindowClosesIn", $"{diff.Days:D2}d : {diff.Hours:D2}h : {diff.Minutes:D2}m")
                    : _loc.Get("Student_RequestWindowClosed");
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task AcceptRequest(PairRequest request)
        {
            var result = await _matchingFlowService.AcceptPairRequestAsync(request.Id, AssignedSupervisorId);
            StatusMessage = result.Success
                ? _loc.Format("Student_AcceptedPaired_Message", request.MenteeName)
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task RejectRequest(PairRequest request)
        {
            var result = await _matchingFlowService.RejectPairRequestAsync(request.Id);
            StatusMessage = result.Success
                ? _loc.Format("Student_RequestRejected_Message", request.MenteeName)
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;
            await LoadAsync();
        }
    }


    // ─── BrowseMentorsViewModel (Phase 1 mentee tab) ──────────────────────────

    public partial class BrowseMentorsViewModel : ObservableObject, INavigatable
    {
        private readonly ILocalizationService _loc;
        public string TabLabel => _loc.Get("Student_TabLabel_BrowseMentors");

        private readonly MatchingFlowService _matchingFlowService;
        private readonly UserStore _userStore;
        private readonly SubjectService _subjectService;

        [ObservableProperty] private ObservableCollection<MentorCard> _mentors = [];
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasStatusMessage;

        public BrowseMentorsViewModel(
            MatchingFlowService matchingFlowService,
            UserStore userStore,
            SubjectService subjectService,
            ILocalizationService loc)
        {
            _matchingFlowService = matchingFlowService;
            _userStore = userStore;
            _subjectService = subjectService;
            _loc = loc;
        }

        public async Task OnNavigatedToAsync() => await LoadAsync();

        public async Task LoadAsync()
        {
            IsLoading = true;
            Mentors.Clear();

            var currentUser = _userStore.User as StudentModel;
            var availableMentors = await _matchingFlowService.GetAvailableMentorsAsync();
            var subjects = (await _subjectService.GetAllSubjectsAsync()).Data ?? [];
            var subjectMap = subjects.ToDictionary(s => s.Id, s => s.Name);

            var pendingMentorIds = new HashSet<int>();
            if (currentUser != null)
            {
                var pendingRequests = await _matchingFlowService.GetPendingRequestsForMenteeAsync(currentUser.Id);
                foreach (var req in pendingRequests)
                    pendingMentorIds.Add(req.MentorId);
            }

            foreach (var mentor in availableMentors.Where(m => m.Id != currentUser?.Id))
            {
                string subjectName = mentor.MentorProfile != null &&
                    subjectMap.TryGetValue(mentor.MentorProfile.SubjectToTeach, out string? sn)
                    ? sn : "N/A";

                Mentors.Add(new MentorCard
                {
                    MentorId = mentor.Id,
                    MentorName = mentor.UserName,
                    ProfilePicturePath = mentor.ProfilePicturePath,
                    Gender = mentor.Gender,
                    SubjectName = subjectName,
                    GradeName = mentor.Grade.Name,
                    ClassNum = mentor.ClassNum,
                    HasPendingRequest = pendingMentorIds.Contains(mentor.Id)
                });
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task SendRequest(MentorCard card)
        {
            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) return;

            var result = await _matchingFlowService.SendPairRequestAsync(currentUser.Id, card.MentorId);
            StatusMessage = result.Success
                ? _loc.Format("Student_RequestSent_Message", card.MentorName)
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;

            if (result.Success)
                card.HasPendingRequest = true;
        }

        [RelayCommand]
        private async Task CancelRequest(MentorCard card)
        {
            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) return;

            var result = await _matchingFlowService.CancelPairRequestAsync(currentUser.Id, card.MentorId);
            StatusMessage = result.Success
                ? _loc.Format("Student_RequestCancelled_Message", card.MentorName)
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;

            if (result.Success)
                card.HasPendingRequest = false;
        }
    }

    public partial class MentorCard : ObservableObject
    {
        public int MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string GradeName { get; set; } = string.Empty;
        public int ClassNum { get; set; }
        public string ProfilePicturePath { get; set; } = string.Empty;
        public Gender Gender { get; set; } = Gender.PreferNoAnswer;
        [ObservableProperty] private bool _hasPendingRequest;
    }
}