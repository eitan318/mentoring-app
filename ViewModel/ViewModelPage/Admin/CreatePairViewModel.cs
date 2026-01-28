using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class CreatePairViewModel : ObservableObject, INavigatable, ICloseable
    {
        [ObservableProperty] private Model.Supervisor? _selectedSupervisor;
        [ObservableProperty] private Model.Student? _selectedMentor;
        [ObservableProperty] private Model.Student? _selectedMentee;

        [ObservableProperty] private ObservableCollection<Model.Supervisor> _availableSupervisors = [];
        [ObservableProperty] private ObservableCollection<Model.Student> _availableMentors = [];
        [ObservableProperty] private ObservableCollection<Model.Student> _availableMentees = [];

        public event Action? RequestClose;

        public CreatePairViewModel() { }

        [RelayCommand]
        private Task CreatePair()
        {
            if (SelectedSupervisor == null || SelectedMentor == null || SelectedMentee == null)
            {
                return Task.CompletedTask;
            }

            RequestClose?.Invoke();
            return Task.CompletedTask;
        }
    }
}