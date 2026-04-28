using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;

namespace MentoringApp.ViewModel.ViewModel.Student
{

    public partial class MentorDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTEE";
        [ObservableProperty] private double _menteeProgress = 50.0;

        public MentorDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueApiClient issueClient,
            ReviewApiClient reviewClient,
            UserStore userStore,
            SettingsApiClient settingsClient,
            PairModel pair,
            UserModel? mentee)
            : base(windowService, navigationService, issueClient, reviewClient, userStore, settingsClient, pair, mentee)
        { }

        [RelayCommand]
        private async Task CreateReview()
        {
            await _navigationService.NavigateToAsync<AddReviewViewModel, int>(Pair.Id);
            await LoadDataAsync();
        }
    }


}
