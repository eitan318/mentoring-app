using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class AdminRegisterViewModel : ObservableObject, INavigatable
    {
        private readonly IFileService _fileService;
        private readonly INavigationService _navigationService;
        private readonly IWindowService _windowService;

        [RelayCommand] private async Task RegisterStudent() => 
            await _windowService.ShowDialogAsync<RegistrationViewModel, bool>(false);
        [RelayCommand] private async Task RegisterSupervisor() => 
            await _windowService.ShowDialogAsync<RegistrationViewModel, bool>(true);

        public AdminRegisterViewModel(IFileService fileService, IWindowService windowService, INavigationService navigationService)
        {
            _fileService = fileService;
            _windowService = windowService;
            _navigationService = navigationService;
        }


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
