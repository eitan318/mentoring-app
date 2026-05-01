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
using MentoringApp.ViewModel.ViewModel.User;
using System.Collections.ObjectModel;
using System.Threading;

namespace MentoringApp.ViewModel.ViewModel.Student;

/// <summary>
/// Student dashboard ViewModel. Dynamically composes the tab list (<see cref="Pairs"/>)
/// based on the current phase and the student's match status.
/// </summary>
public partial class StudentDashboardViewModel : ObservableObject, ViewModelHelper.INavigatable
{
    public ObservableCollection<object> Pairs { get; set; } = new();
    public bool HasNoPairs => Pairs.Count == 0;

    [ObservableProperty] private object? _selectedPair;
    [ObservableProperty] private bool _isPhaseBannerVisible;
    [ObservableProperty] private string _bannerTitle = string.Empty;
    [ObservableProperty] private string _bannerSubtitle = string.Empty;
    [ObservableProperty] private string _bannerTimer = string.Empty;
    [ObservableProperty] private string _bannerColor = "#E3F2FD";
    [ObservableProperty] private string _bannerTextColor = "#1565C0";
    [ObservableProperty] private bool _isPhase1Active;
    [ObservableProperty] private bool _isPhase2Active;

    private CancellationTokenSource? _timerCts;
    private DateTime? _tier1Deadline;
    private DateTime? _tier3Deadline;
    private bool _isPhase1Complete;

    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;
    private readonly UserStore _userStore;
    private readonly PairApiClient _pairClient;
    private readonly IssueApiClient _issueClient;
    private readonly ReviewApiClient _reviewClient;
    private readonly SettingsApiClient _settingsClient;
    private readonly MatchingApiClient _matchingClient;
    private readonly ReferenceApiClient _referenceClient;
    private readonly BrowseMentorsViewModel _browseMentorsVm;
    private readonly SelectionGalleryViewModel _selectionGalleryVm;
    private readonly MentorRequestsViewModel _mentorRequestsVm;

    public StudentDashboardViewModel(
        INavigationService navigationService,
        IWindowService windowService,
        IToastService toastService,
        ILocalizationService loc,
        UserStore userStore,
        PairApiClient pairClient,
        IssueApiClient issueClient,
        ReviewApiClient reviewClient,
        SettingsApiClient settingsClient,
        MatchingApiClient matchingClient,
        ReferenceApiClient referenceClient,
        BrowseMentorsViewModel browseMentorsVm,
        SelectionGalleryViewModel selectionGalleryVm,
        MentorRequestsViewModel mentorRequestsVm)
    {
        _windowService = windowService;
        _toastService = toastService;
        _loc = loc;
        _navigationService = navigationService;
        _userStore = userStore;
        _pairClient = pairClient;
        _issueClient = issueClient;
        _reviewClient = reviewClient;
        _settingsClient = settingsClient;
        _matchingClient = matchingClient;
        _referenceClient = referenceClient;
        _browseMentorsVm = browseMentorsVm;
        _selectionGalleryVm = selectionGalleryVm;
        _mentorRequestsVm = mentorRequestsVm;
    }

    public async Task OnNavigatedToAsync() => await LoadDataAsync();

    private async Task LoadDataAsync()
    {
        Pairs.Clear();
        var currentUser = _userStore.User;
        if (currentUser == null || !currentUser.IsStudent) return;

        var settings = await _settingsClient.GetAllAsync();
        _tier1Deadline = settings.Phase1Deadline != null ? DateTime.Parse(settings.Phase1Deadline) : null;
        _tier3Deadline = settings.Phase2Deadline != null ? DateTime.Parse(settings.Phase2Deadline) : null;
        _isPhase1Complete = settings.IsPhase1Complete;

        SetupTimer();

        bool mentorIsMatched = false;
        bool menteeIsMatched = false;

        var subjects = (await _referenceClient.GetSubjectsAsync()).ToDictionary(s => s.Id, s => s.Name);

        if (currentUser.IsMentor)
        {
            try
            {
                var pair = await _pairClient.GetByMentorAsync(currentUser.Id);
                var vm = new MentorDashboardViewModel(
                    _windowService, _navigationService, _issueClient,
                    _reviewClient, _userStore, _settingsClient, pair, pair.Mentee);
                await vm.LoadDataAsync();
                Pairs.Add(vm);
                mentorIsMatched = true;
            }
            catch { }
        }

        if (currentUser.IsMentee)
        {
            try
            {
                var pair = await _pairClient.GetByMenteeAsync(currentUser.Id);
                var vm = new MenteeDashboardViewModel(
                    _windowService, _navigationService, _issueClient,
                    _reviewClient, _userStore, _settingsClient, pair, pair.Mentor);
                await vm.LoadDataAsync();
                Pairs.Add(vm);
                menteeIsMatched = true;
            }
            catch { }
        }

        if (_isPhase1Complete)
        {
            if (currentUser.IsMentee && !menteeIsMatched)
            {
                await _selectionGalleryVm.LoadAsync();
                Pairs.Add(_selectionGalleryVm);
            }
            if (currentUser.IsMentor && !mentorIsMatched)
            {
                _mentorRequestsVm.IsPhase2Active = true;
                await _mentorRequestsVm.LoadAsync();
                Pairs.Add(_mentorRequestsVm);
            }
        }
        else
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
            await _toastService.ShowInfoAsync(_loc.Get("Student_Phase1Info_Title"), _loc.Get("Student_Phase1Info_Body"));
        else if (IsPhase2Active)
            await _toastService.ShowInfoAsync(_loc.Get("Student_Phase2Info_Title"), _loc.Get("Student_Phase2Info_Body"));
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
        bool hasPhaseTab = Pairs.OfType<BrowseMentorsViewModel>().Any()
                        || Pairs.OfType<MentorRequestsViewModel>().Any()
                        || Pairs.OfType<SelectionGalleryViewModel>().Any();
        if (!hasPhaseTab) { IsPhaseBannerVisible = false; return; }

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
            else BannerTimer = _loc.Get("Student_BannerTimer_PendingActivation");
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
            else BannerTimer = _loc.Get("Student_BannerTimer_PendingAlgorithm");
        }
    }
}



// ─── SelectionGalleryViewModel (Phase 2 mentee tab) ───────────────────────

public partial class SelectionGalleryViewModel : ObservableObject, MentoringApp.ViewModel.ViewModelHelper.INavigatable
{
    private readonly ILocalizationService _loc;
    public string TabLabel => _loc.Get("Student_TabLabel_TopMatches");

    private readonly MatchingApiClient _matchingClient;
    private readonly UserStore _userStore;

    [ObservableProperty] private ObservableCollection<MatchRecommendationResponse> _recommendations = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private bool _alreadyMatched;

    public SelectionGalleryViewModel(MatchingApiClient matchingClient, UserStore userStore, ILocalizationService loc)
    {
        _matchingClient = matchingClient;
        _userStore = userStore;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync() => await LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        Recommendations.Clear();

        var currentUser = _userStore.User;
        if (currentUser == null || !currentUser.IsStudent) { IsLoading = false; return; }

        var recs = await _matchingClient.GetRecommendationsAsync(currentUser.Id);
        foreach (var rec in recs.Take(3))
            Recommendations.Add(rec);

        IsLoading = false;
    }

    [RelayCommand]
    private async Task ChooseMentor(MatchRecommendationResponse recommendation)
    {
        var currentUser = _userStore.User;
        if (currentUser == null) return;

        try
        {
            await _matchingClient.GalleryPickAsync(new GalleryPickRequest(currentUser.Id, recommendation.MentorId, 1));
            AlreadyMatched = true;
            StatusMessage = _loc.Format("Student_MatchedWith_Message", recommendation.MentorName);
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
        HasStatusMessage = true;
    }
}


// ─── MentorRequestsViewModel (Phase 2 mentor tab) ─────────────────────────

public partial class MentorRequestsViewModel : ObservableObject, MentoringApp.ViewModel.ViewModelHelper.INavigatable
{
    private readonly ILocalizationService _loc;
    public string TabLabel => _loc.Get("Student_TabLabel_MentoringRequests");

    private readonly MatchingApiClient _matchingClient;
    private readonly UserStore _userStore;
    private readonly SettingsApiClient _settingsClient;

    [ObservableProperty] private ObservableCollection<PairRequestResponse> _pendingRequests = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private string _requestWindowTimerDisplay = string.Empty;
    [ObservableProperty] private bool _isPhase2Active;

    public int AssignedSupervisorId { get; set; } = 1;

    public MentorRequestsViewModel(MatchingApiClient matchingClient, UserStore userStore, SettingsApiClient settingsClient, ILocalizationService loc)
    {
        _matchingClient = matchingClient;
        _userStore = userStore;
        _settingsClient = settingsClient;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync() => await LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        PendingRequests.Clear();

        var currentUser = _userStore.User;
        if (currentUser == null || !currentUser.IsMentor) { IsLoading = false; return; }

        var requests = await _matchingClient.GetRequestsForMentorAsync(currentUser.Id);
        foreach (var req in requests)
            PendingRequests.Add(req);

        var settings = await _settingsClient.GetAllAsync();
        if (settings.Phase1Deadline != null)
        {
            var deadline = DateTime.Parse(settings.Phase1Deadline);
            var diff = deadline - DateTime.Now;
            RequestWindowTimerDisplay = diff.TotalSeconds > 0
                ? _loc.Format("Student_RequestWindowClosesIn", $"{diff.Days:D2}d : {diff.Hours:D2}h : {diff.Minutes:D2}m")
                : _loc.Get("Student_RequestWindowClosed");
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task AcceptRequest(PairRequestResponse request)
    {
        try
        {
            await _matchingClient.AcceptRequestAsync(request.Id, new AcceptRequestBody(AssignedSupervisorId));
            StatusMessage = _loc.Format("Student_AcceptedPaired_Message", request.MenteeName);
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
        HasStatusMessage = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RejectRequest(PairRequestResponse request)
    {
        try
        {
            await _matchingClient.RejectRequestAsync(request.Id);
            StatusMessage = _loc.Format("Student_RequestRejected_Message", request.MenteeName);
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
        HasStatusMessage = true;
        await LoadAsync();
    }
}


// ─── BrowseMentorsViewModel (Phase 1 mentee tab) ──────────────────────────

public partial class BrowseMentorsViewModel : ObservableObject, MentoringApp.ViewModel.ViewModelHelper.INavigatable
{
    private readonly ILocalizationService _loc;
    public string TabLabel => _loc.Get("Student_TabLabel_BrowseMentors");

    private readonly MatchingApiClient _matchingClient;
    private readonly UserStore _userStore;
    private readonly ReferenceApiClient _referenceClient;

    [ObservableProperty] private ObservableCollection<MentorCard> _mentors = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasStatusMessage;

    public BrowseMentorsViewModel(MatchingApiClient matchingClient, UserStore userStore, ReferenceApiClient referenceClient, ILocalizationService loc)
    {
        _matchingClient = matchingClient;
        _userStore = userStore;
        _referenceClient = referenceClient;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync() => await LoadAsync();

    public async Task LoadAsync()
    {
        IsLoading = true;
        Mentors.Clear();

        var currentUser = _userStore.User;
        var availableMentors = await _matchingClient.GetAvailableMentorsAsync();
        var subjectMap = (await _referenceClient.GetSubjectsAsync()).ToDictionary(s => s.Id, s => s.Name);

        var pendingMentorIds = new HashSet<int>();
        if (currentUser != null)
        {
            var pending = await _matchingClient.GetRequestsForMenteeAsync(currentUser.Id);
            foreach (var req in pending) pendingMentorIds.Add(req.MentorId);
        }

        foreach (var mentor in availableMentors.Where(m => m.Id != currentUser?.Id))
        {
            string subjectName = subjectMap.TryGetValue(mentor.MentorProfile.SubjectToTeach, out string? sn) ? sn : "N/A";

            Mentors.Add(new MentorCard
            {
                MentorId = mentor.Id,
                MentorName = mentor.UserName,
                ProfilePicturePath = mentor.ProfilePicturePath ?? "",
                Gender = mentor.Gender,
                SubjectName = subjectName,
                GradeName = mentor.Grade.Name ?? "",
                ClassNum = mentor.ClassNum,
                HasPendingRequest = pendingMentorIds.Contains(mentor.Id)
            });
        }

        IsLoading = false;
    }

    [RelayCommand]
    private async Task SendRequest(MentorCard card)
    {
        var currentUser = _userStore.User;
        if (currentUser == null) return;
        try
        {
            await _matchingClient.SendPairRequestAsync(new SendPairRequestBody(currentUser.Id, card.MentorId));
            StatusMessage = _loc.Format("Student_RequestSent_Message", card.MentorName);
            card.HasPendingRequest = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
        HasStatusMessage = true;
    }

    [RelayCommand]
    private async Task CancelRequest(MentorCard card)
    {
        var currentUser = _userStore.User;
        if (currentUser == null) return;
        try
        {
            var pending = await _matchingClient.GetRequestsForMenteeAsync(currentUser.Id);
            var req = pending.FirstOrDefault(r => r.MentorId == card.MentorId);
            if (req != null) await _matchingClient.CancelRequestAsync(req.Id);
            StatusMessage = _loc.Format("Student_RequestCancelled_Message", card.MentorName);
            card.HasPendingRequest = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
        }
        HasStatusMessage = true;
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
    public Gender Gender { get; set; }
    [ObservableProperty] private bool _hasPendingRequest;
}
