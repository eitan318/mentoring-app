using CommunityToolkit.Mvvm.ComponentModel;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Supervisor;

public partial class PairDetailsViewModel : ObservableObject, INavigatable<int>
{
    private readonly PairApiClient _pairClient;
    private readonly IssueApiClient _issueClient;
    private readonly ReviewApiClient _reviewClient;
    private readonly SettingsApiClient _settingsClient;

    [ObservableProperty] private PairModel? _currentPair;
    [ObservableProperty] private string _mentorName = "";
    [ObservableProperty] private string _menteeName = "";
    [ObservableProperty] private ObservableCollection<IssueModel> _pairIssues = new();
    [ObservableProperty] private ObservableCollection<ReviewResponse> _pairReviews = new();
    [ObservableProperty] private double _totalMeetingHours;
    [ObservableProperty] private double _requiredMeetingHours = 10;
    [ObservableProperty] private double _hoursProgress;
    [ObservableProperty] private bool _showIssues = true;

    public PairDetailsViewModel(
        PairApiClient pairClient,
        IssueApiClient issueClient,
        ReviewApiClient reviewClient,
        SettingsApiClient settingsClient)
    {
        _pairClient = pairClient;
        _issueClient = issueClient;
        _reviewClient = reviewClient;
        _settingsClient = settingsClient;
    }

    public async Task OnNavigatedToAsync(int pairId)
    {
        CurrentPair = await _pairClient.GetByIdAsync(pairId);

        var reviews = await _reviewClient.GetByPairAsync(pairId);
        PairReviews = new ObservableCollection<ReviewResponse>(reviews);

        var settings = await _settingsClient.GetAllAsync();
        RequiredMeetingHours = settings.MeetingHoursBarrier;
        TotalMeetingHours = PairReviews.Sum(r => r.AmountOfHours);
        HoursProgress = RequiredMeetingHours > 0
            ? Math.Min(100, (TotalMeetingHours / RequiredMeetingHours) * 100) : 0;

        if (CurrentPair != null)
        {
            var mentorIssues = await _issueClient.GetByUserAsync(CurrentPair.Mentor.Id);
            var menteeIssues = await _issueClient.GetByUserAsync(CurrentPair.Mentee.Id);
            var allIssues = mentorIssues.Concat(menteeIssues)
                .GroupBy(i => i.Id).Select(g => g.First())
                .OrderByDescending(i => i.CreationDate);
            PairIssues = new ObservableCollection<IssueModel>(allIssues);
        }
    }
}
