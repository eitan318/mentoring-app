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
        protected readonly ReviewService _reviewService;
        protected readonly SettingsService _settingsService;

        // Cached so we can reload data after returning from sub-pages
        private int _currentSupervisorId;

        [ObservableProperty] private SupervisorModel? _selectedSupervisor;

        
        [ObservableProperty] private ObservableCollection<Pair> _pairsSupervised = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
        [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
        [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
        private ObservableCollection<IssueModel> _allIssues = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FilteredPendingIssues))]
        [NotifyPropertyChangedFor(nameof(FilteredResolvedIssues))]
        [NotifyPropertyChangedFor(nameof(ResolvedIssuesCount))]
        private Pair? _issueFilterPair;

        public IEnumerable<IssueModel> FilteredPendingIssues => AllIssues
            .Where(i => !i.IsResolved)
            .Where(i => IssueFilterPair == null || i.ReportedByUserId == IssueFilterPair.Mentor.Id || i.ReportedByUserId == IssueFilterPair.Mentee.Id);

        public IEnumerable<IssueModel> FilteredResolvedIssues => AllIssues
            .Where(i => i.IsResolved)
            .Where(i => IssueFilterPair == null || i.ReportedByUserId == IssueFilterPair.Mentor.Id || i.ReportedByUserId == IssueFilterPair.Mentee.Id);
            
        public int ResolvedIssuesCount => FilteredResolvedIssues.Count();

        [ObservableProperty] private object? _selectedPaneContent;

        [ObservableProperty] private bool _showResolvedIssues;

        [RelayCommand]
        private void ToggleResolvedIssues() => ShowResolvedIssues = !ShowResolvedIssues;

        public SupervisorDashboardViewModel(
            INavigationService navigationService, 
            PairService pairService, 
            IssueService issueService, 
            UserService userService,
            ReviewService reviewService,
            SettingsService settingsService)
        {
            _navigationService = navigationService;
            _pairService = pairService;
            _issueService = issueService;
            _userService = userService;
            _reviewService = reviewService;
            _settingsService = settingsService;
        }

        [RelayCommand]
        private async Task SelectIssue(IssueModel? issue)
        {
            if (issue != null)
            {
                var vm = new IssueViewModel(_navigationService, _issueService);

                var pair = PairsSupervised.FirstOrDefault(p => p.Mentor.Id == issue.ReportedByUserId || p.Mentee.Id == issue.ReportedByUserId);
                if (pair != null)
                {
                    vm.RelatedPairName = $"Originating from Pair: {pair.Mentor.UserName} & {pair.Mentee.UserName}";
                }

                vm.OnCloseRequested = () => SelectedPaneContent = null;
                vm.OnIssueResolved = () => 
                {
                    SelectedPaneContent = null;
                    _ = LoadSupervisorDataAsync(_currentSupervisorId); // Refresh issues!
                };
                await vm.OnNavigatedToAsync(issue.Id);
                SelectedPaneContent = vm;
            }
        }

        [RelayCommand]
        private async Task SelectPair(Pair? pair)
        {
            IssueFilterPair = pair; // Apply filtering

            if (pair != null)
            {
                var vm = new PairDetailsViewModel(_pairService, _issueService, _reviewService, _settingsService)
                {
                    ShowIssues = false
                };
                await vm.OnNavigatedToAsync(pair.Id);
                SelectedPaneContent = vm;
            }
            else 
            {
                 SelectedPaneContent = null;
            }
        }

        private async Task LoadSupervisorDataAsync(int supervisorId)
        {
            var pairsResult = await _pairService.GetPairsBySupervisorAsync(supervisorId);
            var issuesResult = await _issueService.GetIssuesBySupervisorAsync(supervisorId);

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
