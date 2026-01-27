using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class AllSupervisorsViewModel : ObservableObject, INavigatable
    {
        private readonly INavigationService _navigationService;
        public ObservableCollection<Supervisor> SupervisorsList { get; set; }

        public AllSupervisorsViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            
            // Mock data
            SupervisorsList = new ObservableCollection<Supervisor>
            {
                new Supervisor { UserName = "John Doe", PairsSupervised = 5 },
                new Supervisor { UserName = "Jane Smith", PairsSupervised = 3 }
            };
        }

        [RelayCommand]
        private async Task InspectSupervisor(Supervisor chosen)
        {
            if (chosen != null)
            {
                await _navigationService.NavigateToAsync<SupervisorViewModel, Supervisor>(chosen);
            }
        }

        [RelayCommand]
        private async Task Back() => await _navigationService.GoBackAsync();
    }
}