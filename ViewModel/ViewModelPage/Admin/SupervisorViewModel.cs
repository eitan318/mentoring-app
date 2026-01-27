using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class SupervisorViewModel : ObservableObject, INavigatable<Supervisor>
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty] private Supervisor _selectedSupervisor;
        public SupervisorViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }
        [RelayCommand] private async Task Back() => await _navigationService.GoBackAsync();
        public Task OnNavigatedToAsync(Supervisor supervisor)
        {
            SelectedSupervisor = supervisor;
            return Task.CompletedTask;
        }

    }
}
