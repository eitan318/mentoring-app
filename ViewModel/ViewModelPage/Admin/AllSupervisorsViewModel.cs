using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{

    public class SupervisorSummary
    {
        public required Supervisor Supervisor { get; set; }
        public int PairsCount { get; set; }
        public int PendingIssuesCount { get; set; }
    }


    public partial class AllSupervisorsViewModel : ObservableObject,INavigatable 
    {
        private readonly INavigationService _navigationService;
        public ObservableCollection<SupervisorSummary> SupervisorsList { get; set; }

        public AllSupervisorsViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            SupervisorsList = new ObservableCollection<SupervisorSummary>
            {
                new SupervisorSummary {
                    Supervisor = new Supervisor("John Doe"),
                    PairsCount = 5,
                    PendingIssuesCount = 2
                },
                new SupervisorSummary {
                    Supervisor = new Supervisor("Jane Smith"),
                    PairsCount = 3,
                    PendingIssuesCount = 0
                }
            };
        }

        [RelayCommand]
        private async Task InspectSupervisor(SupervisorSummary chosenSummary)
        {
            if (chosenSummary?.Supervisor != null)
            {
                await _navigationService.NavigateToAsync<SupervisorViewModel, int>(chosenSummary.Supervisor.Id);
            }
        }
    }
}