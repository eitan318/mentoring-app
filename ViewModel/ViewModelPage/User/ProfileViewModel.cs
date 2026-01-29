using MentoringApp.Model;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel; // Using CommunityToolkit for brevity
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Service;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.Store;

namespace MentoringApp.ViewModel.ViewModelPage.User
{
    public partial class ProfileViewModel : ObservableObject, INavigatable
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private Model.User _currentUser;

        public ICommand SaveCommand { get; }

        public ProfileViewModel(UserStore userStore, AuthService authService)
        {
            this._authService = authService;
            if (userStore.User == null)
            {
                throw new Exception("No userrr");
            }
            CurrentUser = userStore.User;
            SaveCommand = new RelayCommand(ExecuteSave);
        }

        private void ExecuteSave()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Saving changes for: {CurrentUser.UserName}");
            }
            catch (System.Exception ex)
            {
                
            }
        }
    }

}