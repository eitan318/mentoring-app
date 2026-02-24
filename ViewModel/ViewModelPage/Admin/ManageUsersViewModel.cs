using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.ViewModelPage.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class ManageUsersViewModel : ObservableObject, INavigatable
    {
        private readonly IFileService _fileService;
        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;

        [RelayCommand] private async Task RegisterStudent() => 
            await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(false);
        [RelayCommand] private async Task RegisterSupervisor() => 
            await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(true);

        [RelayCommand] private async Task DeleteUsers() =>
            await _navigationService.NavigateToAsync<RegistrationViewModel, bool>(true);

        
        public ObservableCollection<Model.User> AllUsers { get; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
        [NotifyCanExecuteChangedFor(nameof(ViewUserCommand))]
        private Model.User? _selectedUser;


        [ObservableProperty]
        private string _searchText = string.Empty;

        private bool HasSelectedUser => SelectedUser != null;
        public ManageUsersViewModel(IFileService fileService, IWindowService windowService, INavigationService navigationService)
        {
            _fileService = fileService;
            _windowService = windowService;
            _navigationService = navigationService;


            AllUsers = new ObservableCollection<Model.User>()
            {
                new Model.Supervisor("hello"),
                new Model.Student("jhi")
            };
        }


        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private void DeleteUser()
        {
            if (SelectedUser != null)
            {
                AllUsers.Remove(SelectedUser);
                SelectedUser = null;
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private void ViewUser()
        {
            if (SelectedUser != null)
            {
                _navigationService.NavigateToAsync<OtherProfileViewModel>();
            }
        }

        public IEnumerable<Model.User> FilteredUsers => string.IsNullOrWhiteSpace(SearchText)
            ? AllUsers
            : AllUsers.Where(u => u.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                  u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        partial void OnSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredUsers));





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
