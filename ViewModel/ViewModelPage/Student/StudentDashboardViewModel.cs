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

        [ObservableProperty] private PairMemberDashboardViewModel? _selectedPair;

        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;
        public StudentDashboardViewModel(INavigationService navigationService, IWindowService windowService)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            Pairs = [];
            LoadData();
        }

        private void LoadData()
        {
            Pairs.Add(new MentorDashboardViewModel(_windowService, _navigationService, 1, 97));
            Pairs.Add(new MenteeDashboardViewModel(1, _navigationService));
        }

    }

    public abstract partial class PairMemberDashboardViewModel : ObservableObject
    {
        public abstract string CounterpartRole { get; }

        [ObservableProperty]
        private Model.Student _counterpart;
        private readonly INavigationService _navigationService;

        protected PairMemberDashboardViewModel(int counterpartId, INavigationService navigationService)
        {
            Counterpart = new Model.Student("John" + counterpartId);
            _navigationService = navigationService;
        }

    }

    public partial class MenteeDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTOR";

        public MenteeDashboardViewModel(int mentorId, INavigationService navigationService) : base(mentorId, navigationService)
        {
        }
    }

    public partial class MentorDashboardViewModel : PairMemberDashboardViewModel
    {
        private readonly IWindowService _windowService;
        public override string CounterpartRole => "MENTEE";

        [ObservableProperty] private double _menteeProgress;

        public ObservableCollection<Issue> MyIssues { get; set; } = new();

        public MentorDashboardViewModel(IWindowService windowService, INavigationService navigationService, int menteeId, double initialProgress) : base(menteeId, navigationService)
        {
            _windowService = windowService;
            MenteeProgress = initialProgress;
        }

        [RelayCommand]
        private async Task AddIssue() =>
            await _windowService.ShowDialogAsync<AddIssueViewModel>();
    }
}
