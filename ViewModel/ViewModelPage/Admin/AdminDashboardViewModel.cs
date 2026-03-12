using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MentoringApp.Service;
using System.Diagnostics;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class AdminDashboardViewModel : ObservableObject, INavigatable
    {
        private readonly INavigationService _navigationService;
        private readonly UserService _userService;

        public ObservableCollection<Model.Supervisor> SupervisorsListPreview { get; set; }

        public AdminDashboardViewModel( INavigationService navigationService, UserService userService)
        {
            _navigationService = navigationService;
            _userService = userService;

            SupervisorsListPreview = new ObservableCollection<Model.Supervisor>();
            _ = LoadSupervisorsPreviewAsync();
        }


        private async Task LoadSupervisorsPreviewAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();

            var supervisorMatches = allUsers?
                .OfType<Model.Supervisor>()
                .Take(4)
                .ToList() ?? new List<Model.Supervisor>();

            SupervisorsListPreview.Clear();
            foreach (var supervisor in supervisorMatches)
            {
                SupervisorsListPreview.Add(supervisor);
            }
        }

        [RelayCommand]
        private async Task InspectSupervisor(Model.Supervisor chosen)
        {
            if (chosen != null)
            {
                await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(chosen.Id);
            }
        }

        [RelayCommand] private async Task ManageUsers() => await _navigationService.NavigateToAsync<ManageUsersViewModel>();
        [RelayCommand] private async Task ViewAllSupervisors() => await _navigationService.NavigateToAsync<AllSupervisorsViewModel>();
        [RelayCommand] private async Task ManagePairs() => await _navigationService.NavigateToAsync<ManagePairsViewModel>();
    }
}