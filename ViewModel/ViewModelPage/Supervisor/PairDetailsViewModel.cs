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

        [ObservableProperty] private Pair? _currentPair;
        [ObservableProperty] private ObservableCollection<Issue> _pairIssues = new();
        [ObservableProperty] private ObservableCollection<Review> _pairReviews = new();

        public PairDetailsViewModel(PairService pairService, IssueService issueService, ReviewService reviewService)
        {
            _pairService = pairService;
            _issueService = issueService;
            _reviewService = reviewService;
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

            if (CurrentPair != null)
            {
                var allIssues = new List<Issue>();
                
                var mentorIssues = await _issueService.GetIssuesByUserAsync(CurrentPair.Mentor.Id);
                if (mentorIssues.Success && mentorIssues.Data != null)
                    allIssues.AddRange(mentorIssues.Data);

                var menteeIssues = await _issueService.GetIssuesByUserAsync(CurrentPair.Mentee.Id);
                if (menteeIssues.Success && menteeIssues.Data != null)
                    allIssues.AddRange(menteeIssues.Data);
                
                PairIssues = new ObservableCollection<Issue>(allIssues.OrderByDescending(i => i.CreationDate));
            }
        }

    }
}
