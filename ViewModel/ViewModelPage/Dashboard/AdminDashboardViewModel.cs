using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage;
using System.Windows.Input;

namespace MentoringApp.ViewModel.ViewModelPage
{
    public class AdminDashboardViewModel : ViewModelBase
    {
        private readonly IWindowService _windowService;
        public ICommand RegisterSupervisorCommand { get; }
        public ICommand RegisterMentorCommand { get; }
        public ICommand RegisterMenteeCommand { get; }
        public ICommand LogoutCommand { get; }

        public AdminDashboardViewModel(IWindowService windowService, INavigationService navigationService)
        {
            _windowService = windowService;
            RegisterSupervisorCommand = new RelayCommand(() => OpenSignup(UserRole.Supervisor));
            RegisterMentorCommand = new RelayCommand(() => OpenSignup(UserRole.Mentor));
            RegisterMenteeCommand = new RelayCommand(() => OpenSignup(UserRole.Mentee));
            LogoutCommand = new RelayCommand(() => navigationService.NavigateToAsync<LoginViewModel>());
        }

        private void OpenSignup(UserRole role)
        {
            _windowService.ShowDialog<RegistrationViewModel>(vm => {
                vm.RegisteringUserRole = role;
            });
        }
    }
}