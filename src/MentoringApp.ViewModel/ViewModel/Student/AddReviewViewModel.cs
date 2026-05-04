using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModel.Student;

public partial class AddReviewViewModel : ObservableValidator, INavigatable<int>, ICloseable
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Please write a review.")]
    [MinLength(10, ErrorMessage = "Review is too short.")]
    private string _reviewContent = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0.1, 24, ErrorMessage = "Please enter between 0.1 and 24 hours.")]
    private double _amountOfHours = 1.0;

    private int _currentPairId;

    private readonly ReviewApiClient _reviewClient;
    private readonly INavigationService _navigationService;
    private readonly UserStore _userStore;

    public event Action? RequestClose;

    public AddReviewViewModel(ReviewApiClient reviewClient, UserStore userStore, INavigationService navigationService)
    {
        _reviewClient = reviewClient;
        _userStore = userStore;
        _navigationService = navigationService;
    }

    public Task OnNavigatedToAsync(int pairId)
    {
        _currentPairId = pairId;
        ReviewContent = string.Empty;
        AmountOfHours = 1.0;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task AddReview()
    {
        ValidateAllProperties();
        if (HasErrors || _currentPairId == 0 || _userStore.User == null) return;

        await _reviewClient.CreateAsync(new CreateReviewRequest(
            Content: ReviewContent,
            Date: DateTime.Now,
            PairId: _currentPairId,
            AuthorUserId: _userStore.User.Id,
            AmountOfHours: AmountOfHours));

        RequestClose?.Invoke();
        await _navigationService.GoBackAsync();
    }
}
