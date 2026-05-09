using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User;

public partial class IssueViewModel : ObservableObject, INavigatable<int>
{
    [ObservableProperty] private IssueModel? _currentIssue;
    [ObservableProperty] private string? _relatedPairName;

    private readonly INavigationService _navigationService;
    private readonly IssueApiClient _issueClient;

    public int? ForwardingsupervisorId { get; set; }
    public bool CanForward => ForwardingsupervisorId.HasValue;

    public Action? OnCloseRequested { get; set; }
    public Action? OnIssueResolved { get; set; }
    public Action? OnIssueForwarded { get; set; }

    public IssueViewModel(INavigationService navigationService, IssueApiClient issueClient)
    {
        _navigationService = navigationService;
        _issueClient = issueClient;
    }

    public virtual async Task OnNavigatedToAsync(int issueId)
    {
        CurrentIssue = await _issueClient.GetByIdAsync(issueId);
    }

    [RelayCommand]
    private async Task Back()
    {
        if (OnCloseRequested != null)
            OnCloseRequested.Invoke();
        else
            await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task ResolveIssue()
    {
        if (CurrentIssue == null || CurrentIssue.IsResolved || CurrentIssue.IsForwardedToAdmin) return;
        try
        {
            await _issueClient.ResolveAsync(CurrentIssue.Id);
            OnIssueResolved?.Invoke();
            if (OnIssueResolved == null) await _navigationService.GoBackAsync();
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
