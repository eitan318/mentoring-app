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
    /// Shows a mentor their incoming pair requests (from Tier 1 and Tier 3).
    /// Allows them to accept or reject each request.
    /// </summary>
    public partial class MentorRequestsViewModel : ObservableObject, INavigatable
    {
        private readonly MatchingFlowService _matchingFlowService;
        private readonly UserStore _userStore;

        [ObservableProperty] private ObservableCollection<PairRequest> _pendingRequests = [];
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string? _statusMessage;
        [ObservableProperty] private bool _hasStatusMessage;

        // Supervisor ID used for creating the pair – injected or looked up externally
        public int AssignedSupervisorId { get; set; } = 1;

        public MentorRequestsViewModel(
            MatchingFlowService matchingFlowService,
            UserStore userStore)
        {
            _matchingFlowService = matchingFlowService;
            _userStore = userStore;
        }

        public async Task OnNavigatedToAsync()
        {
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            PendingRequests.Clear();

            var currentUser = _userStore.User as StudentModel;
            if (currentUser == null || !currentUser.IsMentor)
            {
                IsLoading = false;
                return;
            }

            var requests = await _matchingFlowService.GetPendingRequestsForMentorAsync(currentUser.Id);
            foreach (var req in requests)
                PendingRequests.Add(req);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task AcceptRequest(PairRequest request)
        {
            var result = await _matchingFlowService.AcceptPairRequestAsync(request.Id, AssignedSupervisorId);

            StatusMessage = result.Success
                ? $"✓ Accepted! You are now paired with {request.MenteeName}."
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;

            await LoadAsync(); // Refresh list
        }

        [RelayCommand]
        private async Task RejectRequest(PairRequest request)
        {
            var result = await _matchingFlowService.RejectPairRequestAsync(request.Id);

            StatusMessage = result.Success
                ? $"Request from {request.MenteeName} rejected."
                : $"✗ {result.ErrorMessage}";
            HasStatusMessage = true;

            await LoadAsync(); // Refresh list
        }
    }
}
