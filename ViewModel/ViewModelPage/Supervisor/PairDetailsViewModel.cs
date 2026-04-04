using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.Supervisor
{
    public partial class PairDetailsViewModel : ObservableObject, INavigatable<int>
    {
        private readonly PairService _pairService;
        private readonly IssueService _issueService;
        private readonly ReviewService _reviewService;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private Pair? _currentPair;
        [ObservableProperty] private ObservableCollection<IssueModel> _pairIssues = new();
        [ObservableProperty] private ObservableCollection<Review> _pairReviews = new();
        [ObservableProperty] private double _totalMeetingHours;
        [ObservableProperty] private double _requiredMeetingHours = 10;
        [ObservableProperty] private double _hoursProgress;  // 0-100 for ProgressBar
        [ObservableProperty] private bool _showIssues = true;

        public PairDetailsViewModel(PairService pairService, IssueService issueService, ReviewService reviewService, SettingsService settingsService)
        {
            _pairService = pairService;
            _issueService = issueService;
            _reviewService = reviewService;
            _settingsService = settingsService;
        }

        public async Task OnNavigatedToAsync(int pairId)
        {
            var pairResult = await _pairService.GetPairById(pairId);
            if (pairResult.Success && pairResult.Data != null)
            {
                CurrentPair = pairResult.Data;
            }

            var reviewsResult = await _reviewService.GetReviewsByPairAsync(pairId);
            if (reviewsResult.Success && reviewsResult.Data != null)
            {
                PairReviews = new ObservableCollection<Review>(reviewsResult.Data);
            }

            // Calculate meeting hours progress
            RequiredMeetingHours = await _settingsService.GetMeetingHoursBarrierAsync();
            TotalMeetingHours = PairReviews.Sum(r => r.AmountOfHours);
            HoursProgress = RequiredMeetingHours > 0
                ? Math.Min(100, (TotalMeetingHours / RequiredMeetingHours) * 100)
                : 0;

            if (CurrentPair != null)
            {
                var allIssues = new List<IssueModel>();
                
                var mentorIssues = await _issueService.GetIssuesByUserAsync(CurrentPair.Mentor.Id);
                if (mentorIssues.Success && mentorIssues.Data != null)
                    allIssues.AddRange(mentorIssues.Data);

                var menteeIssues = await _issueService.GetIssuesByUserAsync(CurrentPair.Mentee.Id);
                if (menteeIssues.Success && menteeIssues.Data != null)
                    allIssues.AddRange(menteeIssues.Data);
                
                var distinctIssues = allIssues.GroupBy(i => i.Id).Select(g => g.First());
                PairIssues = new ObservableCollection<IssueModel>(distinctIssues.OrderByDescending(i => i.CreationDate));
            }
        }

    }
}
