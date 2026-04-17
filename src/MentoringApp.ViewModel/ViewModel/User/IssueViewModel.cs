using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModel.User
{
    public partial class IssueViewModel : ObservableObject, INavigatable<int>
    {
        [ObservableProperty] private IssueModel _currentIssue;
        [ObservableProperty] private string? _relatedPairName;

        private readonly INavigationService _navigationService;
        private readonly IssueService _issueService;

        /// <summary>Set to the supervisor's user ID to enable the "Forward to Admin" button.</summary>
        public int? ForwardingsupervisorId { get; set; }
        public bool CanForward => ForwardingsupervisorId.HasValue;

        public Action? OnCloseRequested { get; set; }
        public Action? OnIssueResolved { get; set; }
        public Action? OnIssueForwarded { get; set; }

        public IssueViewModel(INavigationService navigationService, IssueService issueService)
        {
            _navigationService = navigationService;
            _issueService = issueService;
        }

        public virtual async Task OnNavigatedToAsync(int issueId)
        {
            CurrentIssue = (await _issueService.GetIssueByIdAsync(issueId)).Data;
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
            if (CurrentIssue != null && !CurrentIssue.IsResolved)
            {
                var result = await _issueService.ResolveIssueAsync(CurrentIssue.Id);
                if (result.Success)
                {
                    if (OnIssueResolved != null)
                        OnIssueResolved.Invoke();
                    else
                        await _navigationService.GoBackAsync();
                }
            }
        }

        [RelayCommand]
        private async Task ForwardToAdmin()
        {
            if (CurrentIssue == null || !CanForward || CurrentIssue.IsForwardedToAdmin) return;
            var result = await _issueService.ForwardIssueAsync(CurrentIssue.Id, ForwardingsupervisorId!.Value);
            if (result.Success)
            {
                CurrentIssue.ForwardedBySupervisorId = ForwardingsupervisorId;
                OnPropertyChanged(nameof(CurrentIssue));
                OnIssueForwarded?.Invoke();
            }
        }
    }
}