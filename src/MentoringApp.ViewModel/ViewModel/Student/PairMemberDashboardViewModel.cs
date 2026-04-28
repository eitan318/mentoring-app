using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModel.Student
{

    // ─── Pair dashboard base ───────────────────────────────────────────────────
    public abstract partial class PairMemberDashboardViewModel : ObservableObject
    {
        public abstract string CounterpartRole { get; }

        protected readonly IWindowService _windowService;
        protected readonly INavigationService _navigationService;
        protected readonly IssueApiClient _issueClient;
        protected readonly ReviewApiClient _reviewClient;
        protected readonly UserStore _userStore;
        protected readonly SettingsApiClient _settingsClient;

        [ObservableProperty] private UserModel? _counterpart;
        public bool HasSupervisor => false; // Supervisor info not directly available from PairResponse

        public PairModel Pair { get; }

        public ObservableCollection<IssueModel> MyIssues { get; set; } = new();
        public ObservableCollection<ReviewResponse> RecentReviews { get; set; } = new();

        [ObservableProperty] private double _totalMeetingHours;
        [ObservableProperty] private double _requiredMeetingHours = 10;
        [ObservableProperty] private double _hoursProgress;

        protected PairMemberDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueApiClient issueClient,
            ReviewApiClient reviewClient,
            UserStore userStore,
            SettingsApiClient settingsClient,
            PairModel pair,
            UserModel? counterpart)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            _issueClient = issueClient;
            _reviewClient = reviewClient;
            _userStore = userStore;
            _settingsClient = settingsClient;
            Pair = pair;
            Counterpart = counterpart;
        }

        public virtual async Task LoadDataAsync()
        {
            var issues = await _issueClient.GetByUserAsync(_userStore.User!.Id);
            MyIssues = new ObservableCollection<IssueModel>(issues);

            var reviews = await _reviewClient.GetByPairAsync(Pair.Id);
            RecentReviews = new ObservableCollection<ReviewResponse>(
                reviews.OrderByDescending(r => r.Date));

            var settings = await _settingsClient.GetAllAsync();
            RequiredMeetingHours = settings.MeetingHoursBarrier;
            TotalMeetingHours = RecentReviews.Sum(r => r.AmountOfHours);
            HoursProgress = RequiredMeetingHours > 0
                ? Math.Min(100, (TotalMeetingHours / RequiredMeetingHours) * 100) : 0;

            OnPropertyChanged(nameof(MyIssues));
            OnPropertyChanged(nameof(RecentReviews));
        }

        [RelayCommand]
        private async Task IssueToSupervisor()
        {
            var categories = await _issueClient.GetCategoriesAsync();
            await _navigationService.NavigateToAsync<AddIssueViewModel, IEnumerable<IssueCategoryModel>>(categories);
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            if (Counterpart != null)
                await _navigationService.NavigateToAsync<OtherProfileViewModel, int>(Counterpart.Id);
        }
    }

}
