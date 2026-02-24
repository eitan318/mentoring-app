using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class CreatePairViewModel : ObservableObject, INavigatable
    {
        // Add NotifyCanExecuteChangedFor to refresh the button state automatically
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
        private Model.Supervisor? _selectedSupervisor;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
        private Model.Student? _selectedMentor;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
        private Model.Student? _selectedMentee;

        // Raw Data
        [ObservableProperty] private ObservableCollection<Model.Supervisor> _availableSupervisors = [];
        [ObservableProperty] private ObservableCollection<Model.Student> _availableMentors = [];
        [ObservableProperty] private ObservableCollection<Model.Student> _availableMentees = [];

        // Search Strings
        [ObservableProperty] private string _supervisorSearchText = string.Empty;
        [ObservableProperty] private string _mentorSearchText = string.Empty;
        [ObservableProperty] private string _menteeSearchText = string.Empty;

        public CreatePairViewModel()
        {
            // Sample Data
            AvailableSupervisors = [new Model.Supervisor("Dr. Smith"), new Model.Supervisor("Prof. Jones")];
            AvailableMentors = [new Model.Student("Alice"), new Model.Student("Alex")];
            AvailableMentees = [new Model.Student("Bob"), new Model.Student("Charlie")];
        }

        // Filtered Properties
        public IEnumerable<Model.Supervisor> FilteredSupervisors =>
            AvailableSupervisors.Where(x => x.UserName?.Contains(SupervisorSearchText, StringComparison.OrdinalIgnoreCase) ?? true);

        public IEnumerable<Model.Student> FilteredMentors =>
            AvailableMentors.Where(x => x.UserName?.Contains(MentorSearchText, StringComparison.OrdinalIgnoreCase) ?? true);

        public IEnumerable<Model.Student> FilteredMentees =>
            AvailableMentees.Where(x => x.UserName?.Contains(MenteeSearchText, StringComparison.OrdinalIgnoreCase) ?? true);

        partial void OnSupervisorSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredSupervisors));
        partial void OnMentorSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredMentors));
        partial void OnMenteeSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredMentees));

        // The logic for validation
        private bool CanCreatePair => SelectedSupervisor != null &&
                                     SelectedMentor != null &&
                                     SelectedMentee != null;

        [RelayCommand(CanExecute = nameof(CanCreatePair))]
        private async Task CreatePair()
        {
            // Logic to save pair here
            // e.g., await _dataService.SavePair(SelectedSupervisor, SelectedMentor, SelectedMentee);

            await Task.CompletedTask;
        }
    }
}