using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.Data.Interfaces;
using MentoringApp.Service;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class CreatePairViewModel : ObservableObject, INavigatable
    {
        // Add NotifyCanExecuteChangedFor to refresh the button state automatically
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
        private SupervisorModel? _selectedSupervisor;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
        private StudentModel? _selectedMentor;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
        private StudentModel? _selectedMentee;

        // Raw Data
        [ObservableProperty] private ObservableCollection<SupervisorModel> _availableSupervisors = [];
        [ObservableProperty] private ObservableCollection<StudentModel> _availableMentors = [];
        [ObservableProperty] private ObservableCollection<StudentModel> _availableMentees = [];

        // Search Strings
        [ObservableProperty] private string _supervisorSearchText = string.Empty;
        [ObservableProperty] private string _mentorSearchText = string.Empty;
        [ObservableProperty] private string _menteeSearchText = string.Empty;

        private readonly UserService _userService;
        private readonly PairService _pairService;

        public CreatePairViewModel(UserService userService, PairService pairService)
        {
            _userService = userService;
            _pairService = pairService;
            LoadAvailableUsers();
        }

        private async Task LoadAvailableUsers()
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var allPairsResult = await _pairService.GetAllPairsAsync();
            var allPairs = allPairsResult.Success && allPairsResult.Data != null ? allPairsResult.Data : [];

            var pairedMentorIds = allPairs.Select(p => p.Mentor.Id).ToHashSet();
            var pairedMenteeIds = allPairs.Select(p => p.Mentee.Id).ToHashSet();

            var supervisors = allUsers.OfType<SupervisorModel>().ToList();
            var mentors = allUsers.OfType<StudentModel>().Where(s => s.IsMentor && !pairedMentorIds.Contains(s.Id)).ToList();
            var mentees = allUsers.OfType<StudentModel>().Where(s => s.IsMentee && !pairedMenteeIds.Contains(s.Id)).ToList();

            AvailableSupervisors = new ObservableCollection<SupervisorModel>(supervisors);
            AvailableMentors = new ObservableCollection<StudentModel>(mentors);
            AvailableMentees = new ObservableCollection<StudentModel>(mentees);

            OnPropertyChanged(nameof(FilteredSupervisors));
            OnPropertyChanged(nameof(FilteredMentors));
            OnPropertyChanged(nameof(FilteredMentees));

            SelectedMentee = null;
            SelectedMentor = null;
            SelectedSupervisor = null;
        }

        // Filtered Properties
        public IEnumerable<SupervisorModel> FilteredSupervisors =>
            AvailableSupervisors.Where(x => x.UserName?.Contains(SupervisorSearchText, StringComparison.OrdinalIgnoreCase) ?? true);

        public IEnumerable<StudentModel> FilteredMentors =>
            AvailableMentors.Where(x => x.UserName?.Contains(MentorSearchText, StringComparison.OrdinalIgnoreCase) ?? true);

        public IEnumerable<StudentModel> FilteredMentees =>
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
            if (SelectedSupervisor is not null && SelectedMentee is not null && SelectedMentor  is not null) {
                await _pairService.CreatePairAsync(SelectedSupervisor.Id, SelectedMentor.Id, SelectedMentee.Id);
                LoadAvailableUsers();
            }
        }
    }
}