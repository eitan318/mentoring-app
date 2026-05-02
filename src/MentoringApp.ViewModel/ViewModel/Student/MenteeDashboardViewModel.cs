using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModel.Student
{
    public partial class MenteeDashboardViewModel : PairMemberDashboardViewModel
    {
        public override string CounterpartRole => "MENTOR";
        [ObservableProperty] private string _mentorSubject = "Assigned Mentor";

        public MenteeDashboardViewModel(
            IWindowService windowService,
            INavigationService navigationService,
            IssueApiClient issueClient,
            ReviewApiClient reviewClient,
            UserStore userStore,
            SettingsApiClient settingsClient,
            PairModel pair,
            UserModel? mentor)
            : base(windowService, navigationService, issueClient, reviewClient, userStore, settingsClient, pair, mentor)
        { }
    }

}
