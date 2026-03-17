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
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

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

        
        public ObservableCollection<Model.User> AllUsers { get; set; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
        [NotifyCanExecuteChangedFor(nameof(ViewUserCommand))]
        private Model.User? _selectedUser;


        [ObservableProperty]
        private string _searchText = string.Empty;

        private bool HasSelectedUser => SelectedUser != null;
        private readonly UserService _userService;

        public ManageUsersViewModel(IFileService fileService, IWindowService windowService, INavigationService navigationService, UserService userService)
        {
            _fileService = fileService;
            _windowService = windowService;
            _navigationService = navigationService;
            _userService = userService;

        }


        public async Task OnNavigatedToAsync()
        {
            var users = await _userService.GetAllUsersAsync();
            AllUsers = new(users);
        }

        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private async Task DeleteUser()
        {
            if (SelectedUser != null)
            {

                await _userService.DeleteUserAsync(SelectedUser.Id);
                AllUsers.Remove(SelectedUser);
                SelectedUser = null;
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedUser))]
        private async Task ViewUser()
        {
            if (SelectedUser != null)
            {
                await _navigationService.NavigateToAsync<OtherProfileViewModel, int>(SelectedUser.Id);
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
