using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.Student
{
    public partial class StudentDashboardViewModel : ObservableObject, INavigatable
    {
        public ObservableCollection<PairMemberDashboardViewModel> Pairs { get; set; }

        [ObservableProperty]
        private PairMemberDashboardViewModel? _selectedPair;

        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;

        public StudentDashboardViewModel(INavigationService navigationService, IWindowService windowService)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            Pairs = [];
            LoadData();
            SelectedPair = Pairs[0];
        }

        private void LoadData()
        {
            Pairs.Add(new MentorDashboardViewModel(_windowService, _navigationService, 1, 72));
            Pairs.Add(new MentorDashboardViewModel(_windowService, _navigationService, 2, 45));
            Pairs.Add(new MenteeDashboardViewModel(1, _navigationService, _windowService));
        }
    }


    public abstract partial class PairMemberDashboardViewModel : ObservableObject
    {
        public abstract string CounterpartRole { get; }

        private readonly IWindowService _windowService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private Model.Student _counterpart;

        public ObservableCollection<Issue> MyIssues { get; } = new();
        public ObservableCollection<Review> RecentReviews { get; } = new();

        protected PairMemberDashboardViewModel(int counterpartId, INavigationService navigationService, IWindowService windowService)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            ///Counterpart = new Model.Student("Alex Smith " + counterpartId);

            RecentReviews.Add(new Review("Solid understanding of MVVM patterns demonstrated this session.", DateTime.Now.AddDays(-3)));
            RecentReviews.Add(new Review("Good progress on XAML data binding — work on converters next.", DateTime.Now.AddDays(-10)));
            RecentReviews.Add(new Review("Completed the async/await exercise ahead of schedule. Well done.", DateTime.Now.AddDays(-17)));

            MyIssues.Add(new Issue("Difficulty with MVVM RelayCommand wiring", new IssueCategory("Tech", 1), false));
            MyIssues.Add(new Issue("Unclear on DI container registration order", new IssueCategory("Tech", 1), false));
            MyIssues.Add(new Issue("Scheduling conflict for next week's session", new IssueCategory("Admin", 2), true));
        }

        [RelayCommand]
        private async Task IssueToSupervisor() => await _navigationService.NavigateToAsync<AddIssueViewModel>();
    }

    public partial class MenteeDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTOR";

        [ObservableProperty]
        private string _mentorSubject;

        public MenteeDashboardViewModel(int mentorId, INavigationService navigationService, IWindowService windowService)
            : base(mentorId, navigationService, windowService)
        {
            MentorSubject = "Advanced C# and WPF";
        }
    }

    public partial class MentorDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTEE";

        [ObservableProperty]
        private double _menteeProgress;

        private readonly IWindowService _windowService;

        public MentorDashboardViewModel(IWindowService windowService, INavigationService navigationService, int menteeId, double initialProgress)
            : base(menteeId, navigationService, windowService)
        {
            _windowService = windowService;
            MenteeProgress = initialProgress;
        }

        [RelayCommand]
        private async Task CreateReview()
        {
            RecentReviews.Insert(0, new Review($"Session review — {DateTime.Now:MMM d}: Good engagement, reviewed error handling patterns.", DateTime.Now));
            await Task.CompletedTask;
        }
    }
}