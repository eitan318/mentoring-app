using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public partial class CreatePairViewModel : ObservableObject, INavigatable
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
    private UserResponse? _selectedSupervisor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
    private UserResponse? _selectedMentor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
    private UserResponse? _selectedMentee;

    partial void OnSelectedMenteeChanged(UserResponse? value)
    {
        if (value?.GradeId != null)
        {
            var match = AvailableSupervisors.FirstOrDefault(s =>
                s.GradeId == value.GradeId && s.ClassNum == value.ClassNum);
            if (match != null) SelectedSupervisor = match;
        }
    }

    [ObservableProperty] private ObservableCollection<UserResponse> _availableSupervisors = [];
    [ObservableProperty] private ObservableCollection<UserResponse> _availableMentors = [];
    [ObservableProperty] private ObservableCollection<UserResponse> _availableMentees = [];

    [ObservableProperty] private string _supervisorSearchText = string.Empty;
    [ObservableProperty] private string _mentorSearchText = string.Empty;
    [ObservableProperty] private string _menteeSearchText = string.Empty;

    private readonly UserApiClient _userClient;
    private readonly PairApiClient _pairClient;

    public CreatePairViewModel(UserApiClient userClient, PairApiClient pairClient)
    {
        _userClient = userClient;
        _pairClient = pairClient;
        _ = LoadAvailableUsersAsync();
    }

    private async Task LoadAvailableUsersAsync()
    {
        var allUsers = await _userClient.GetAllAsync();
        var allPairs = await _pairClient.GetAllAsync();

        var pairedMentorIds  = allPairs.Select(p => p.MentorId).ToHashSet();
        var pairedMenteeIds  = allPairs.Select(p => p.MenteeId).ToHashSet();

        AvailableSupervisors = new ObservableCollection<UserResponse>(allUsers.Where(u => u.IsSupervisor));
        AvailableMentors     = new ObservableCollection<UserResponse>(allUsers.Where(u => u.IsStudent && u.IsMentor && !pairedMentorIds.Contains(u.Id)));
        AvailableMentees     = new ObservableCollection<UserResponse>(allUsers.Where(u => u.IsStudent && u.IsMentee && !pairedMenteeIds.Contains(u.Id)));

        OnPropertyChanged(nameof(FilteredSupervisors));
        OnPropertyChanged(nameof(FilteredMentors));
        OnPropertyChanged(nameof(FilteredMentees));

        SelectedMentee = null;
        SelectedMentor = null;
        SelectedSupervisor = null;
    }

    public IEnumerable<UserResponse> FilteredSupervisors =>
        AvailableSupervisors.Where(x => x.UserName.Contains(SupervisorSearchText, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<UserResponse> FilteredMentors =>
        AvailableMentors.Where(x => x.UserName.Contains(MentorSearchText, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<UserResponse> FilteredMentees =>
        AvailableMentees.Where(x => x.UserName.Contains(MenteeSearchText, StringComparison.OrdinalIgnoreCase));

    partial void OnSupervisorSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredSupervisors));
    partial void OnMentorSearchTextChanged(string v)     => OnPropertyChanged(nameof(FilteredMentors));
    partial void OnMenteeSearchTextChanged(string v)     => OnPropertyChanged(nameof(FilteredMentees));

    private bool CanCreatePair => SelectedSupervisor != null && SelectedMentor != null && SelectedMentee != null;

    [RelayCommand(CanExecute = nameof(CanCreatePair))]
    private async Task CreatePair()
    {
        if (SelectedSupervisor is null || SelectedMentee is null || SelectedMentor is null) return;
        await _pairClient.CreateAsync(new CreatePairRequest(SelectedSupervisor.Id, SelectedMentor.Id, SelectedMentee.Id));
        await LoadAvailableUsersAsync();
    }

    public Task OnNavigatedToAsync() => LoadAvailableUsersAsync();
}
