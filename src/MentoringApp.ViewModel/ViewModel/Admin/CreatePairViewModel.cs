using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model.User;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

/// <summary>Backs the admin "Create Pair" dialog: pick an available mentor and mentee and manually form a pair.</summary>
public partial class CreatePairViewModel : ObservableObject, INavigatable
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
    private UserModel? _selectedMentor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreatePairCommand))]
    private UserModel? _selectedMentee;

    [ObservableProperty] private ObservableCollection<StudentModel> _availableMentors = [];
    [ObservableProperty] private ObservableCollection<StudentModel> _availableMentees = [];

    [ObservableProperty] private string _mentorSearchText = string.Empty;
    [ObservableProperty] private string _menteeSearchText = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

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

        var pairedMentorIds = allPairs.Select(p => p.Mentor.Id).ToHashSet();
        var pairedMenteeIds = allPairs.Select(p => p.Mentee.Id).ToHashSet();

        AvailableMentors = new ObservableCollection<StudentModel>(
            allUsers.OfType<StudentModel>()
                    .Where(s => s.IsMentor && !pairedMentorIds.Contains(s.Id)));

        AvailableMentees = new ObservableCollection<StudentModel>(
            allUsers.OfType<StudentModel>()
                    .Where(s => s.IsMentee && !pairedMenteeIds.Contains(s.Id)));

        OnPropertyChanged(nameof(FilteredMentors));
        OnPropertyChanged(nameof(FilteredMentees));

        SelectedMentee = null;
        SelectedMentor = null;
    }

    public IEnumerable<UserModel> FilteredMentors =>
        AvailableMentors.Where(x => x.UserName.Contains(MentorSearchText, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<UserModel> FilteredMentees =>
        AvailableMentees.Where(x => x.UserName.Contains(MenteeSearchText, StringComparison.OrdinalIgnoreCase));

    partial void OnMentorSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredMentors));
    partial void OnMenteeSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredMentees));

    private bool CanCreatePair => SelectedMentor != null && SelectedMentee != null;

    [RelayCommand(CanExecute = nameof(CanCreatePair))]
    private async Task CreatePair()
    {
        if (SelectedMentee is null || SelectedMentor is null) return;
        ErrorMessage = string.Empty;
        try
        {
            await _pairClient.CreateAsync(new CreatePairRequest(SelectedMentor.Id, SelectedMentee.Id));
            await LoadAvailableUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public Task OnNavigatedToAsync() => LoadAvailableUsersAsync();
}
