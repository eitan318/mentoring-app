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

namespace MentoringApp.ViewModel.ViewModelPage.Supervisor
{
    public partial class SupervisorDashboardViewModel : ObservableObject, INavigatable<int>
    {
        protected readonly INavigationService _navigationService;
        protected readonly PairService _pairService;
        protected readonly IssueService _issueService;
        protected readonly UserService _userService;

        // Cached so we can reload data after returning from sub-pages
        private int _currentSupervisorId;

        [ObservableProperty] private SupervisorModel? _selectedSupervisor;

        
        [ObservableProperty] private ObservableCollection<Pair> _pairsSupervised = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PendingIssues))]
        [NotifyPropertyChangedFor(nameof(ResolvedIssues))]
        private ObservableCollection<IssueModel> _allIssues = [];
        public IEnumerable<IssueModel> PendingIssues => AllIssues.Where(i => !i.IsResolved);
        public IEnumerable<IssueModel> ResolvedIssues => AllIssues.Where(i => i.IsResolved);


        public SupervisorDashboardViewModel(INavigationService navigationService, PairService pairService, IssueService issueService, UserService userService)
        {
            _navigationService = navigationService;
            _pairService = pairService;
            _issueService = issueService;
            _userService = userService;
        }

        [RelayCommand]
        private async Task SelectIssue(IssueModel? issue)
        {
            if (issue != null)
            {
                await _navigationService.NavigateToAsync<IssueViewModel, int>(issue.Id);
            }
        }

        [RelayCommand]
        private async Task SelectPair(Pair? pair)
        {
            if (pair != null)
            {
                await _navigationService.NavigateToAsync<PairDetailsViewModel, int>(pair.Id);
            }
        }

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            var pairsResult = await _pairService.GetPairsBySupervisorAsync(supervisorId);
            var issuesResult = await _issueService.GetAllIssuesAsync();

            // Swapping the instance now triggers the UI update
            if (pairsResult.Success)
            {
                PairsSupervised = new ObservableCollection<Pair>(pairsResult.Data ?? []);
            }

            if (issuesResult.Success)
            {
                AllIssues = new ObservableCollection<IssueModel>(issuesResult.Data ?? []);
            }
        }


        // Called by GoBackAsync when the user returns to this view
        public new async Task OnNavigatedToAsync()
        {
            await LoadSupervisorDataAsync(_currentSupervisorId);
        }

        public virtual async Task OnNavigatedToAsync(int supervisorId)
        {
            _currentSupervisorId = supervisorId;
            await LoadSupervisorDataAsync(supervisorId);
            Result<UserModel> res = await _userService.GetUserByIdAsync(supervisorId);
            SelectedSupervisor = res.Data as SupervisorModel;
        }
    }
}
