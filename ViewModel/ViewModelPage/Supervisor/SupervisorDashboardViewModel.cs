using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
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

        [ObservableProperty] private Model.Supervisor? _selectedSupervisor;

        public ObservableCollection<Pair> PairsSupervised { get; set; } = [];
        public ObservableCollection<Issue> AllIssues { get; set; } = [];

        public SupervisorDashboardViewModel(INavigationService navigationService, PairService pairService, IssueService issueService, UserService userService)
        {
            _navigationService = navigationService;
            _pairService = pairService;
            _issueService = issueService;
            _userService = userService;
        }

        [RelayCommand]
        private async Task SelectIssue(Issue? issue)
        {
            if (issue != null)
            {
                await _navigationService.NavigateToAsync<IssueViewModel, int>(issue.Id);
            }
        }

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            var pairsResult = await _pairService.GetPairsBySupervisorAsync(supervisorId);
            var issuesResult = await _issueService.GetAllIssuesAsync();

            if (pairsResult.Success && pairsResult.Data != null)
            {
                PairsSupervised = new ObservableCollection<Pair>(pairsResult.Data);
            }
            if(issuesResult.Success && issuesResult.Data != null)
            {
                AllIssues = new ObservableCollection<Issue>(issuesResult.Data);
            }

        }
        public virtual async Task OnNavigatedToAsync(int supervisorId)
        {
            await LoadSupervisorDataAsync(supervisorId);
            Result<Model.User> res = await _userService.GetUserByIdAsync(supervisorId);
            SelectedSupervisor = res.Data as Model.Supervisor;
        }
    }
}
