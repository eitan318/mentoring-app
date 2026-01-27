using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        private readonly IWindowService _windowService;
        public ICommand RegisterSupervisorCommand { get; }
        public ICommand RegisterStudentCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminDashboardViewModel(IWindowService windowService, INavigationService navigationService)
        {
            _windowService = windowService;
            RegisterSupervisorCommand = new RelayCommand(() => OpenSignup(true));
            RegisterStudentCommand = new RelayCommand(() => OpenSignup(false));
            LogoutCommand = new RelayCommand(() => navigationService.NavigateToAsync<LoginViewModel>());
        }

        private void OpenSignup(bool supervisorOrStudentIsSupervisor)
        {
            _windowService.ShowDialog<RegistrationViewModel>(vm => {
                vm.SupervisorOrStudentIsSupervisor = supervisorOrStudentIsSupervisor;
            });
        }
    }
}