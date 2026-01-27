using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class CreatePairViewModel : ObservableObject, INavigatable, ICloseable
    {
        [ObservableProperty] private Supervisor? _selectedSupervisor;
        [ObservableProperty] private Student? _selectedMentor;
        [ObservableProperty] private Student? _selectedMentee;

        [ObservableProperty] private ObservableCollection<Supervisor> _availableSupervisors = new();
        [ObservableProperty] private ObservableCollection<Student> _availableMentors = new();
        [ObservableProperty] private ObservableCollection<Student> _availableMentees = new();

        public event Action? RequestClose;

        public CreatePairViewModel() { }

        [RelayCommand]
        private async Task CreatePair()
        {
            if (SelectedSupervisor == null || SelectedMentor == null || SelectedMentee == null)
            {
                return;
            }

            RequestClose?.Invoke();
        }
    }
}