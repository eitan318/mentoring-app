using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class IssueViewModel : ObservableObject, INavigatable<int>
    {
        [ObservableProperty] private Issue _currentIssue;

        private readonly INavigationService _navigationService;

        public IssueViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public virtual async Task OnNavigatedToAsync(int issueId)
        {
            CurrentIssue = new Issue("Issue #" + issueId, new IssueCategory("Technical Support"), false)
            {
                Id = issueId,
                CreationDate = DateTime.Now
            };
        }

        [RelayCommand]
        private async Task Back()
        {
            await _navigationService.GoBackAsync();
        }
    }
}