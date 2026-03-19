using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class IssueViewModel : ObservableObject, INavigatable<int>
    {
        [ObservableProperty] private IssueModel _currentIssue;

        private readonly INavigationService _navigationService;
        private readonly IssueService _issueService;

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
                    await _navigationService.GoBackAsync();
                }
            }
        }
    }
}