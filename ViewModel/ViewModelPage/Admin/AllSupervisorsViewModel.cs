using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using System.Collections.ObjectModel;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{

    public class SupervisorSummary
    {
        public required Model.Supervisor Supervisor { get; set; }
        public int PairsCount { get; set; }
        public int PendingIssuesCount { get; set; }
    }


    public partial class AllSupervisorsViewModel : ObservableObject,INavigatable 
    {
        private readonly INavigationService _navigationService;
        private readonly UserService _userService;
        private readonly PairService _pairService;
        private readonly IssueService _issueService;
        public ObservableCollection<SupervisorSummary> SupervisorsList { get; set; }

        public AllSupervisorsViewModel(INavigationService navigationService, UserService  userService, PairService pairService, IssueService issueService)
        {
            _navigationService = navigationService;
            _userService = userService;
            _pairService = pairService;
            _issueService = issueService;

            SupervisorsList = new ObservableCollection<SupervisorSummary>();
            LoadSupervisors();
        }

        private async Task LoadSupervisors()
        {
            var supervisors = _userService.GetAllUsersAsync().Result.OfType<Model.Supervisor>();
            foreach (var supervisor in supervisors)
            {

                int pairsCount = _pairService.GetPairsBySupervisor(supervisor.Id).Data.Count();
                int pendingIssuesCount =  _issueService.GetIssuesBySupervisor(supervisor.Id).Data.Count();
                
                // TODO: Fetch real PairsCount and PendingIssuesCount when available in DB/Repos
                SupervisorsList.Add(new SupervisorSummary
                {
                    Supervisor = supervisor,
                    PairsCount = 0,
                    PendingIssuesCount = 0
                });
            }
        }


        [RelayCommand]
        private async Task InspectSupervisor(SupervisorSummary chosenSummary)
        {
            if (chosenSummary?.Supervisor != null)
            {
                await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(chosenSummary.Supervisor.Id);
            }
        }
    }
}