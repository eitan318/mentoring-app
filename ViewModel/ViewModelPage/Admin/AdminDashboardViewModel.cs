
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Auth;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class AdminDashboardViewModel : ObservableObject, INavigatable
    {
        private readonly INavigationService _navigationService;
        public ObservableCollection<Model.Supervisor> SupervisorsListPreview { get; set; }

        public AdminDashboardViewModel( INavigationService navigationService)
        {
            _navigationService = navigationService;

            SupervisorsListPreview = new ObservableCollection<Model.Supervisor>();
            SupervisorsListPreview.Add(new Model.Supervisor("Name1"));
            SupervisorsListPreview.Add(new Model.Supervisor("Name2"));
            SupervisorsListPreview.Add(new Model.Supervisor("Name3"));
            SupervisorsListPreview.Add(new Model.Supervisor("Name4"));
        }

        [RelayCommand]
        private async Task InspectSupervisor(Model.Supervisor chosen)
        {
            if (chosen != null)
            {
                await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(chosen.Id);
            }
        }

        [RelayCommand] private async Task RegisterUsers() => await _navigationService.NavigateToAsync<AdminRegisterViewModel>();
        [RelayCommand] private async Task Logout() => await _navigationService.NavigateToAsync<LoginViewModel>();

        [RelayCommand] private async Task ViewAllSupervisors() => await _navigationService.NavigateToAsync<AllSupervisorsViewModel>();
        [RelayCommand] private async Task ManagePairs() => await _navigationService.NavigateToAsync<ManagePairsViewModel>();
    }
}