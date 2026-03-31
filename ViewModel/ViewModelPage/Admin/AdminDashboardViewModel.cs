using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.Service;
using MentoringApp.Model.User;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class AdminDashboardViewModel : ObservableObject, INavigatable
    {
        private readonly INavigationService _navigationService;
        private readonly UserService _userService;
        private readonly IFileService _fileService;
        private readonly ExcelImportService _excelImportService;

        [ObservableProperty]
        private string _statusMessage = "";

        public ObservableCollection<SupervisorModel> SupervisorsListPreview { get; set; }

        public AdminDashboardViewModel(INavigationService navigationService, UserService userService, IFileService fileService, ExcelImportService excelImportService)
        {
            _navigationService = navigationService;
            _userService = userService;
            _fileService = fileService;
            _excelImportService = excelImportService;


        }
        public async Task OnNavigatedToAsync()
        {
            SupervisorsListPreview = new ObservableCollection<SupervisorModel>();
            _ = LoadSupervisorsPreviewAsync();
        }



        private async Task LoadSupervisorsPreviewAsync()
        {
            var allUsers = await _userService.GetAllUsersAsync();

            var supervisorMatches = allUsers?
                .OfType<SupervisorModel>()
                .ToList() ?? new List<SupervisorModel>();

            SupervisorsListPreview.Clear();
            foreach (var supervisor in supervisorMatches)
            {
                SupervisorsListPreview.Add(supervisor);
            }
        }

        [RelayCommand]
        private async Task InspectSupervisor(SupervisorModel chosen)
        {
            if (chosen != null)
            {
                await _navigationService.NavigateToAsync<SupervisorDashboardViewModel, int>(chosen.Id);
            }
        }

        [RelayCommand] private async Task ManageUsers() => await _navigationService.NavigateToAsync<ManageUsersViewModel>();
        [RelayCommand] private async Task ManagePairs() => await _navigationService.NavigateToAsync<ManagePairsViewModel>();

        
    }
}