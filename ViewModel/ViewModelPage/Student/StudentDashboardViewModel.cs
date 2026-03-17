using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.Service;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelPage.User;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModelPage.Student
{
    public partial class StudentDashboardViewModel : ObservableObject, INavigatable
    {
        public ObservableCollection<PairMemberDashboardViewModel> Pairs { get; set; } = new();

        [ObservableProperty]
        private PairMemberDashboardViewModel? _selectedPair;

        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;
        private readonly UserStore _userStore;
        private readonly PairService _pairService;
        private readonly IssueService _issueService;
        private readonly ReviewService _reviewService;

        public StudentDashboardViewModel(
            INavigationService navigationService, 
            IWindowService windowService,
            UserStore userStore,
            PairService pairService,
            IssueService issueService,
            ReviewService reviewService)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            _userStore = userStore;
            _pairService = pairService;
            _issueService = issueService;
            _reviewService = reviewService;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            Pairs.Clear();
            var currentUser = _userStore.User as Model.Student;
            if (currentUser == null) return;

            if (currentUser.IsMentor)
            {
                var result = await _pairService.GetPairByMentorAsync(currentUser.Id);
                if (result.Success && result.Data != null)
                {
                    var pair = result.Data;
                    var mentorVm = new MentorDashboardViewModel(_windowService, _navigationService, _issueService, _reviewService, _userStore, pair);
                    await mentorVm.LoadDataAsync();
                    Pairs.Add(mentorVm);
                }
            }
            if (currentUser.IsMentee)
            {
                var result = await _pairService.GetPairByMenteeAsync(currentUser.Id);
                if (result.Success && result.Data != null)
                {
                    var pair = result.Data;
                    var menteeVm = new MenteeDashboardViewModel(_windowService, _navigationService, _issueService, _reviewService, _userStore, pair);
                    await menteeVm.LoadDataAsync();
                    Pairs.Add(menteeVm);
                }
            }

            if (Pairs.Count > 0)
            {
                SelectedPair = Pairs[0];
            }
        }
    }


    public abstract partial class PairMemberDashboardViewModel : ObservableObject
    {
        public abstract string CounterpartRole { get; }

        protected readonly IWindowService _windowService;
        protected readonly INavigationService _navigationService;
        protected readonly IssueService _issueService;
        protected readonly ReviewService _reviewService;
        protected readonly UserStore _userStore;

        [ObservableProperty]
        private Model.Student _counterpart;

        public Pair Pair { get; }

        public ObservableCollection<Issue> MyIssues { get; set; } = new();
        public ObservableCollection<Review> RecentReviews { get; set; } = new();

        protected PairMemberDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueService issueService,
            ReviewService reviewService,
            UserStore userStore,
            Pair pair,
            Model.Student counterpart)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            _issueService = issueService;
            _reviewService = reviewService;
            _userStore = userStore;
            Pair = pair;
            Counterpart = counterpart;
        }

        public virtual async Task LoadDataAsync()
        {
            var issuesResult = await _issueService.GetIssuesByUserAsync(_userStore.User!.Id);
            if (issuesResult.Success && issuesResult.Data != null)
            {
                MyIssues = new ObservableCollection<Issue>(issuesResult.Data);
            }

            var reviewsResult = await _reviewService.GetReviewsByPairAsync(Pair.Id);
            if (reviewsResult.Success && reviewsResult.Data != null)
            {
                var sortedReviews = reviewsResult.Data.OrderByDescending(r => r.Date).ToList();
                RecentReviews = new ObservableCollection<Review>(sortedReviews);
            }
            
            OnPropertyChanged(nameof(MyIssues));
            OnPropertyChanged(nameof(RecentReviews));

            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task IssueToSupervisor()
        {
            var categoriesResult = await _issueService.GetCategoriesAsync();
            if (categoriesResult.Success && categoriesResult.Data != null)
            {
                await _navigationService.NavigateToAsync<AddIssueViewModel, IEnumerable<IssueCategory>>(categoriesResult.Data);
            }
            else
            {
                await _navigationService.NavigateToAsync<AddIssueViewModel>();
            }
        }
    }

    public partial class MenteeDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTOR";

        [ObservableProperty]
        private string _mentorSubject;

        public MenteeDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueService issueService,
            ReviewService reviewService,
            UserStore userStore,
            Pair pair)
            : base(windowService, navigationService, issueService, reviewService, userStore, pair, pair.Mentor)
        {
            // Note: Since MentorProfile uses SubjectToTeach (an int ID we didn't fetch the string for), 
            // for now we set it based on whatever is available or default if complex
            MentorSubject = "Assigned Mentor";
        }
    }

    public partial class MentorDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTEE";

        [ObservableProperty]
        private double _menteeProgress;

        public MentorDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueService issueService,
            ReviewService reviewService,
            UserStore userStore,
            Pair pair)
            : base(windowService, navigationService, issueService, reviewService, userStore, pair, pair.Mentee)
        {
            MenteeProgress = 50.0;
        }

        [RelayCommand]
        private async Task CreateReview()
        {
            await _navigationService.NavigateToAsync<AddReviewViewModel, Pair>(Pair);
            // After returning, reload to show new reviews
            await LoadDataAsync();
        }
    }
}