using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Service;
using MentoringApp.ViewModel.ViewModelHelper;
using MentoringApp.ViewModel.Store;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModelPage.Student
{
    /// <summary>
    /// Tier 3 – Selection Gallery.
    /// Shown to mentees after Tier 1 deadline. Displays top 3 recommended mentors.
    /// </summary>
    public partial class SelectionGalleryViewModel : ObservableObject, INavigatable
    {
        private readonly MatchingFlowService _matchingFlowService;
        private readonly UserStore _userStore;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private ObservableCollection<MatchScore> _recommendations = [];
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasStatusMessage;
        [ObservableProperty] private bool _alreadyMatched;

        public SelectionGalleryViewModel(
            MatchingFlowService matchingFlowService,
            UserStore userStore,
            SettingsService settingsService)
        {
            _matchingFlowService = matchingFlowService;
            _userStore = userStore;
            _settingsService = settingsService;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Recommendations.Clear();

            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null)
            {
                IsLoading = false;
                return;
            }

            var topRecs = await _matchingFlowService.GetTopRecommendationsAsync(currentUser.Id, topN: 3);
            foreach (var rec in topRecs)
                Recommendations.Add(rec);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task ChooseMentor(MatchScore recommendation)
        {
            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null) return;

            // Use supervisor ID 1 as default; in a real scenario this would be assigned
            // We'd look up which supervisor manages this user's group
            int supervisorId = 1;

            var result = await _matchingFlowService.GalleryPickAsync(currentUser.Id, recommendation.MentorId, supervisorId);

            if (result.Success)
            {
                AlreadyMatched = true;
                StatusMessage = $"✓ You are now matched with {recommendation.MentorName}!";
            }
            else
            {
                StatusMessage = $"✗ {result.ErrorMessage}";
            }
            HasStatusMessage = true;
        }
    }
}
