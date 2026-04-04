using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.Student
{
    /// <summary>
    /// Tier 1 – Browse active mentors and send pair requests.
    /// Shown to mentees before the Tier 1 deadline.
    /// </summary>
    public partial class BrowseMentorsViewModel : ObservableObject, INavigatable
    {
        private readonly MatchingFlowService _matchingFlowService;
        private readonly UserStore _userStore;
        private readonly SubjectService _subjectService;

        [ObservableProperty] private ObservableCollection<MentorCard> _mentors = [];
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasStatusMessage;

        public BrowseMentorsViewModel(
            MatchingFlowService matchingFlowService,
            UserStore userStore,
            SubjectService subjectService)
        {
            _matchingFlowService = matchingFlowService;
            _userStore = userStore;
            _subjectService = subjectService;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Mentors.Clear();

            var availableMentors = await _matchingFlowService.GetAvailableMentorsAsync();
            var subjects = (await _subjectService.GetAllSubjectsAsync()).Data ?? [];
            var subjectMap = subjects.ToDictionary(s => s.Id, s => s.Name);

            foreach (var mentor in availableMentors)
            {
                string subjectName = mentor.MentorProfile != null &&
                    subjectMap.TryGetValue(mentor.MentorProfile.SubjectToTeach, out string? sn)
                    ? sn : "N/A";

                Mentors.Add(new MentorCard
                {
                    MentorId = mentor.Id,
                    MentorName = mentor.UserName,
                    ProfilePicturePath = mentor.ProfilePicturePath,
                    SubjectName = subjectName,
                    GradeName = mentor.Grade.Name
                });
            }

            IsLoading = false;
        }

        [RelayCommand]
        private async Task SendRequest(MentorCard card)
        {
            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) return;

            var result = await _matchingFlowService.SendPairRequestAsync(currentUser.Id, card.MentorId);

            StatusMessage = result.Success
                ? $"✓ Request sent to {card.MentorName}! Waiting for their response."
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;

            if (result.Success)
            {
                // Refresh to remove the mentor if they're now pending/matched
                await LoadAsync();
            }
        }
    }

    public class MentorCard
    {
        public int MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string GradeName { get; set; } = string.Empty;
        public string ProfilePicturePath { get; set; } = string.Empty;
    }
}
