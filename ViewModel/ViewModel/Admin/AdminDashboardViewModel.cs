using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace MentoringApp.ViewModel.ViewModel.Admin
{
    /// <summary>
    /// Admin dashboard ViewModel. Manages the two-phase matching lifecycle:
    /// Phase 1 (registration + manual requests) and Phase 2 (scored selection gallery + auto-match fallback).
    /// A DispatcherTimer fires every second to keep the active deadline countdown current.
    /// <c>IsSelectionPhaseActive</c>/<c>IsProcessComplete</c> flags gate which controls are visible.
    /// </summary>
    public partial class AdminDashboardViewModel : ObservableObject, INavigatable
    {
        private readonly INavigationService _navigationService;
        private readonly UserService _userService;
        private readonly MatchingFlowService _matchingFlowService;
        private readonly SettingsService _settingsService;
        private readonly DispatcherTimer _uiUpdateTimer;

        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private string _operationResult = string.Empty;
        [ObservableProperty] private bool _hasOperationResult;

        // ── Phase State ───────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPhase1Active))]
        [NotifyPropertyChangedFor(nameof(IsPhase2Active))]
        private bool _isSelectionPhaseActive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPhase1Active))]
        [NotifyPropertyChangedFor(nameof(IsPhase2Active))]
        private bool _isProcessComplete;

        public bool IsPhase1Active => !IsSelectionPhaseActive && !IsProcessComplete;
        public bool IsPhase2Active => IsSelectionPhaseActive && !IsProcessComplete;

        // ── Phase Descriptions (short, shown inline) ──────────────────────────

        public string Phase1Summary => "Students log in and fill their details. Mentees can send requests to mentors, who can accept and get paired.";
        public string Phase2Summary => "Unpaired mentees see their top 3 mentor matches and choose one. Remaining unpaired users are auto-matched at the deadline.";

        // ── Active Deadline (single set of controls, VM swaps the data) ───────

        [ObservableProperty] private DateTime? _deadlineInput = DateTime.Now.AddDays(1);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDeadlineScheduled))]
        [NotifyPropertyChangedFor(nameof(IsDeadlineNotScheduled))]
        private DateTime? _activeDeadline;

        [ObservableProperty] private string _deadlineTimeRemaining = string.Empty;

        public bool IsDeadlineScheduled => ActiveDeadline.HasValue;
        public bool IsDeadlineNotScheduled => !ActiveDeadline.HasValue;

        // ── Supervisors ───────────────────────────────────────────────────────

        public ObservableCollection<SupervisorModel> SupervisorsListPreview { get; } = new();

        // ── Constructor ───────────────────────────────────────────────────────

        public AdminDashboardViewModel(
            INavigationService navigationService,
            UserService userService,
            MatchingFlowService matchingFlowService,
            SettingsService settingsService)
        {
            _navigationService = navigationService;
            _userService = userService;
            _matchingFlowService = matchingFlowService;
            _settingsService = settingsService;

            _uiUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uiUpdateTimer.Tick += OnTimerTick;
            _uiUpdateTimer.Start();
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!ActiveDeadline.HasValue) { DeadlineTimeRemaining = string.Empty; return; }
            var remaining = ActiveDeadline.Value - DateTime.Now;
            DeadlineTimeRemaining = remaining.TotalSeconds <= 0
                ? "00:00:00 — Pending"
                : $"{remaining.Days}d {remaining.Hours:D2}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s";
        }

        // ── Navigation Entry ──────────────────────────────────────────────────

        public async Task OnNavigatedToAsync() => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            // First-time setup: redirect admin to school configuration before anything else.
            bool schoolConfigured = await _settingsService.GetIsSchoolConfiguredAsync();
            if (!schoolConfigured)
            {
                await _navigationService.NavigateToAsync<SystemSettingsViewModel>();
                return;
            }

            IsSelectionPhaseActive = await _settingsService.GetIsPhase1CompleteAsync();
            IsProcessComplete = await _settingsService.GetIsProcessCompleteAsync();

            ActiveDeadline = IsSelectionPhaseActive
                ? await _settingsService.GetPhase2DeadlineAsync()
                : await _settingsService.GetPhase1DeadlineAsync();

            if (ActiveDeadline.HasValue) DeadlineInput = ActiveDeadline;

            var allUsers = await _userService.GetAllUsersAsync();
            SupervisorsListPreview.Clear();
            foreach (var supervisor in allUsers.OfType<SupervisorModel>())
                SupervisorsListPreview.Add(supervisor);
        }

        // ── Shared Deadline Commands ──────────────────────────────────────────

        [RelayCommand]
        private async Task SaveDeadline()
        {
            if (!DeadlineInput.HasValue) return;
            ActiveDeadline = DeadlineInput.Value;

            if (IsSelectionPhaseActive)
                await _settingsService.SetPhase2DeadlineAsync(ActiveDeadline.Value);
            else
                await _settingsService.SetPhase1DeadlineAsync(ActiveDeadline.Value);

            ShowResult("✓ Deadline scheduled.");
        }

        [RelayCommand]
        private async Task CancelDeadline()
        {
            ActiveDeadline = null;

            if (IsSelectionPhaseActive)
                await _settingsService.ClearPhase2DeadlineAsync();
            else
                await _settingsService.ClearPhase1DeadlineAsync();
        }

        // ── Phase Action Commands ─────────────────────────────────────────────

        [RelayCommand]
        private async Task StartSelectionPhase()
        {
            ShowResult("Generating suitability scores and opening selection gallery…");

            var result = await _matchingFlowService.GenerateScoreMatrixAsync();
            if (result.Success)
            {
                IsSelectionPhaseActive = true;
                await _settingsService.SetIsPhase1CompleteAsync(true);

                // Swap deadline controls over to Phase 2
                ActiveDeadline = await _settingsService.GetPhase2DeadlineAsync();
                DeadlineInput = DateTime.Now.AddDays(1);

                ShowResult("✓ Scores generated. Mentees can now pick from their top 3 matches.");
            }
            else
            {
                ShowResult($"✗ Failed to start Phase 2: {result.ErrorMessage}");
            }
        }

        [RelayCommand]
        private async Task RunAutoMatch()
        {
            ShowResult("Running final algorithmic auto-match…");

            var result = await _matchingFlowService.RunAutoMatchAsync();
            if (result.Success)
            {
                var fallback = await _matchingFlowService.RunFallbackMatchAsync();
                string fallbackText = fallback.Success
                    ? $"{fallback.Data} fallback pairs assigned."
                    : "Fallback step failed.";

                IsProcessComplete = true;
                await _settingsService.SetIsProcessCompleteAsync(true);

                ShowResult($"✓ Process complete. {result.Data} algorithmic pairs created. {fallbackText}");
            }
            else
            {
                ShowResult($"✗ Auto-match failed: {result.ErrorMessage}");
            }
        }

        // ── Info Dialogs ──────────────────────────────────────────────────────

        [RelayCommand]
        private void ShowPhase1Info() =>
            MessageBox.Show(
                "Students log in and fill in their details.\n\n" +
                "Mentees can browse available mentors and send pairing requests. " +
                "A mentor who accepts becomes paired with that mentee, and the pair's supervisor " +
                "is automatically set to the mentee's class teacher.\n\n" +
                "When you are satisfied with the manual pairings (or the deadline passes), " +
                "move on to Phase 2 to handle any remaining unpaired mentees.",
                "Phase 1 — Registration & Manual Pairing",
                MessageBoxButton.OK, MessageBoxImage.Information);

        [RelayCommand]
        private void ShowPhase2Info() =>
            MessageBox.Show(
                "Every mentee who completed their profile but was not yet paired is shown " +
                "their top 3 algorithmically ranked mentor matches and can choose one.\n\n" +
                "When the deadline passes (or you press 'Run now'), any mentee who still " +
                "hasn't chosen is matched automatically by the algorithm. " +
                "A fallback pass then handles any remaining edge cases.",
                "Phase 2 — Selection Gallery & Auto-Match",
                MessageBoxButton.OK, MessageBoxImage.Information);

        // ── Navigation Commands ───────────────────────────────────────────────

        [RelayCommand] private async Task ManageUsers() => await _navigationService.NavigateToAsync<ManageUsersViewModel>();
        [RelayCommand] private async Task ManagePairs() => await _navigationService.NavigateToAsync<ManagePairsViewModel>();
        [RelayCommand] private async Task SystemSettings() => await _navigationService.NavigateToAsync<SystemSettingsViewModel>();

        [RelayCommand]
        private async Task InspectSupervisor(SupervisorModel chosen)
        {
            if (chosen != null)
                await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(chosen.Id);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ShowResult(string message)
        {
            OperationResult = message;
            HasOperationResult = true;
        }
    }
}