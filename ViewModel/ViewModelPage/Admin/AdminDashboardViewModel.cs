
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.Auth;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class AdminDashboardViewModel : ObservableObject, INavigatable
    {
        private readonly IWindowService _windowService;
        private readonly IFileService _fileService;
        private readonly INavigationService _navigationService;
        public ObservableCollection<Supervisor> SupervisorsListPreview { get; set; }

        public AdminDashboardViewModel(IFileService fileService, IWindowService windowService, INavigationService navigationService)
        {
            _fileService = fileService;
            _windowService = windowService;
            _navigationService = navigationService;

            SupervisorsListPreview = new ObservableCollection<Supervisor>();
            SupervisorsListPreview.Add(new Supervisor("Name1"));
            SupervisorsListPreview.Add(new Supervisor("Name2"));
            SupervisorsListPreview.Add(new Supervisor("Name3"));
            SupervisorsListPreview.Add(new Supervisor("Name4"));
        }

        [RelayCommand]
        private async Task InspectSupervisor(Supervisor chosen)
        {
            if (chosen != null)
            {
                await _navigationService.NavigateToAsync<SupervisorViewModel, int>(chosen.Id);
            }
        }

        [RelayCommand] private async Task Back() => await _navigationService.GoBackAsync();
        [RelayCommand] private async Task Logout() => await _navigationService.NavigateToAsync<LoginViewModel>();
        [RelayCommand] private async Task RegisterStudent() => 
            await _windowService.ShowDialogAsync<RegistrationViewModel, bool>(false);
        [RelayCommand] private async Task RegisterSupervisor() => 
            await _windowService.ShowDialogAsync<RegistrationViewModel, bool>(true);
        [RelayCommand] private async Task ViewAllSupervisors() => await _navigationService.NavigateToAsync<AllSupervisorsViewModel>();
        [RelayCommand] private async Task ManagePairs() => await _navigationService.NavigateToAsync<ManagePairsViewModel>();



        [RelayCommand]
        private void RegisterFromFile()
        {
            string selectedFile = _fileService.OpenFile("Text files (*.txt)|*.txt");

            if (!string.IsNullOrEmpty(selectedFile))
            {
                // Logic to process the file
                System.Diagnostics.Debug.WriteLine($"Selected: {selectedFile}");
            }
        }
    }
}