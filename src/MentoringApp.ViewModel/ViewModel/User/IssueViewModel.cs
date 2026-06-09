using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User;

/// <summary>Backs the issue-detail view: loads a single issue by id and lets a supervisor resolve or forward it.</summary>
public partial class IssueViewModel : ObservableObject, INavigatable<int>, ICloseable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanResolve))]
    private IssueModel? _currentIssue;
    [ObservableProperty] private string? _relatedPairName;
    [ObservableProperty] private string? _reporterName;

    /// <summary>
    /// Supervisors cannot resolve forwarded issues (they've escalated them).
    /// Admins (ForwardingSupervisorId = null) can always resolve, including forwarded ones.
    /// </summary>
    public bool CanResolve => CurrentIssue != null
        && !CurrentIssue.IsResolved
        && (!CurrentIssue.IsForwardedToAdmin || !CanForward);

    private readonly INavigationService _navigationService;
    private readonly IssueApiClient _issueClient;
    private readonly UserApiClient _userClient;
    private readonly PairApiClient _pairClient;

    public int? ForwardingsupervisorId { get; set; }
    public bool CanForward => ForwardingsupervisorId.HasValue;

    // ICloseable — wired by WindowService when shown as a dialog
    public event Action? RequestClose;

    // Callbacks for supervisor pane mode (not used when shown as a dialog)
    public Action? OnCloseRequested { get; set; }
    public Action? OnIssueResolved { get; set; }
    public Action? OnIssueForwarded { get; set; }

    public IssueViewModel(
        INavigationService navigationService,
        IssueApiClient issueClient,
        UserApiClient userClient,
        PairApiClient pairClient)
    {
        _navigationService = navigationService;
        _issueClient = issueClient;
        _userClient = userClient;
        _pairClient = pairClient;
    }

    public virtual async Task OnNavigatedToAsync(int issueId)
    {
        CurrentIssue = await _issueClient.GetByIdAsync(issueId);
        if (CurrentIssue == null) return;

        // Load reporter name
        try
        {
            var reporter = await _userClient.GetByIdAsync(CurrentIssue.ReportedByUserId);
            ReporterName = reporter?.UserName;
        }
        catch { /* reporter may be deleted; leave null */ }

        // Load the pair the reporter belongs to (they may be mentor or mentee in the pair)
        if (string.IsNullOrEmpty(RelatedPairName))
        {
            try
            {
                PairModel? pair = null;
                try { pair = await _pairClient.GetByMentorAsync(CurrentIssue.ReportedByUserId); } catch { }
                if (pair == null)
                    try { pair = await _pairClient.GetByMenteeAsync(CurrentIssue.ReportedByUserId); } catch { }

                if (pair != null)
                    RelatedPairName = $"{pair.Mentor.UserName} ↔ {pair.Mentee.UserName}";
            }
            catch { /* reporter not in any pair */ }
        }
    }

    [RelayCommand]
    private async Task Back()
    {
        // Dialog mode (admin): close the window via ICloseable
        RequestClose?.Invoke();
        // Pane mode (supervisor): collapse the pane
        OnCloseRequested?.Invoke();
        // Navigation mode: go back
        if (RequestClose == null && OnCloseRequested == null)
            await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task ResolveIssue()
    {
        // CanResolve encapsulates all guard logic (not resolved, not forwarded-unless-admin)
        if (!CanResolve) return;
        try
        {
            await _issueClient.ResolveAsync(CurrentIssue!.Id);

            // Supervisor pane mode: fire callback so pane reloads/closes
            OnIssueResolved?.Invoke();

            // Dialog mode (admin): close the window — WindowService will call LoadDataAsync after
            RequestClose?.Invoke();

            // Navigation mode: go back (only if neither callback is wired)
            if (OnIssueResolved == null && RequestClose == null)
                await _navigationService.GoBackAsync();
        }
        catch { }
    }

    [RelayCommand]
    private async Task ForwardToAdmin()
    {
        if (CurrentIssue == null || !CanForward || CurrentIssue.IsForwardedToAdmin || CurrentIssue.IsResolved) return;
        try
        {
            await _issueClient.ForwardAsync(CurrentIssue.Id, new ForwardIssueRequest(ForwardingsupervisorId!.Value));
            CurrentIssue = await _issueClient.GetByIdAsync(CurrentIssue.Id);
            OnPropertyChanged(nameof(CurrentIssue));
            OnIssueForwarded?.Invoke();
        }
        catch { }
    }
}
