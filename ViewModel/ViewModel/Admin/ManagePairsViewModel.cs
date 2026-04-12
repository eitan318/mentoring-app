using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModel.Supervisor;

namespace MentoringApp.ViewModel.ViewModel.Admin
{
    /// <summary>
    /// Admin pair management ViewModel. Displays all pairs with search filtering.
    /// Selecting a pair eagerly loads its review count and unresolved issue count
    /// for display in the sidebar before navigating to the full <see cref="PairDetailsViewModel"/>.
    /// </summary>
    public partial class ManagePairsViewModel : ObservableObject, INavigatable
    {
        private readonly IWindowService _windowService;
        private readonly INavigationService _navigationService;
        private readonly PairService _pairService;
        private readonly ReviewService _reviewService;
        private readonly IssueService _issueService;

        // ── All pairs (source of truth) ─────────────────────────────────────
        public ObservableCollection<Pair> AllPairs { get; set; } = [];

        // ── Selected pair (drives the sidebar) ─────────────────────────────
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SeparateCommand))]
        [NotifyCanExecuteChangedFor(nameof(SelectPairCommand))]
        private Pair? _selectedPair;

        [ObservableProperty] private int _selectedPairReviewCount;
        [ObservableProperty] private int _selectedPairActiveIssueCount;
        [ObservableProperty] private bool _isLoadingDetails;

        private bool HasSelectedPair => SelectedPair != null;

        // ── Search ──────────────────────────────────────────────────────────
        [ObservableProperty] private string _searchText = string.Empty;

        public System.Collections.Generic.IEnumerable<Pair> FilteredPairs
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    return AllPairs;

                return AllPairs.Where(p =>
                    p.Mentor.UserName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ||
                    p.Mentee.UserName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        partial void OnSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredPairs));

        // ── Constructor ─────────────────────────────────────────────────────
        public ManagePairsViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            PairService pairService,
            ReviewService reviewService,
            IssueService issueService)
        {
            _windowService     = windowService;
            _navigationService = navigationService;
            _pairService       = pairService;
            _reviewService     = reviewService;
            _issueService      = issueService;
        }

        public async Task OnNavigatedToAsync() => await RefreshPairs();

        private async Task RefreshPairs()
        {
            var res = await _pairService.GetAllPairsAsync();
            AllPairs = new ObservableCollection<Pair>(res.Data ?? []);
            SelectedPair = null;
            OnPropertyChanged(nameof(FilteredPairs));
        }

        async partial void OnSelectedPairChanged(Pair? value)
        {
            if (value == null) return;

            IsLoadingDetails = true;
            SelectedPairReviewCount = 0;
            SelectedPairActiveIssueCount = 0;

            // Load reviews
            var revRes = await _reviewService.GetReviewsByPairAsync(value.Id);
            if (revRes.Success && revRes.Data != null)
                SelectedPairReviewCount = revRes.Data.Count();

            // Load active issues (unresolved) for mentor and mentee
            int issueCount = 0;
            var mentorIssues = await _issueService.GetIssuesByUserAsync(value.Mentor.Id);
            if (mentorIssues.Success && mentorIssues.Data != null)
                issueCount += mentorIssues.Data.Count(i => !i.IsResolved);

            var menteeIssues = await _issueService.GetIssuesByUserAsync(value.Mentee.Id);
            if (menteeIssues.Success && menteeIssues.Data != null)
                issueCount += menteeIssues.Data.Count(i => !i.IsResolved);

            SelectedPairActiveIssueCount = issueCount;
            IsLoadingDetails = false;
        }

        // ── Commands ────────────────────────────────────────────────────────
        [RelayCommand]
        private void CreatePair() => _navigationService.NavigateToAsync<CreatePairViewModel>();

        [RelayCommand(CanExecute = nameof(HasSelectedPair))]
        private async Task SelectPair()
        {
            if (SelectedPair != null)
                await _navigationService.NavigateToAsync<PairDetailsViewModel, int>(SelectedPair.Id);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedPair))]
        private async Task Separate()
        {
            if (SelectedPair is null) return;
            var confirm = MessageBox.Show(
                "Are you sure you want to separate this pair? This action cannot be undone.",
                "Confirm Separate", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _pairService.SeparatePairAsync(SelectedPair.Id);
            await RefreshPairs();
        }
    }
}