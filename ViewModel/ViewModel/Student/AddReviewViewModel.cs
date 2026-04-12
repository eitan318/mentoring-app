using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModel.Student
{
    public partial class AddReviewViewModel : ObservableValidator, INavigatable<Pair>, ICloseable
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

        private Pair? _currentPair;
        
        private readonly ReviewService _reviewService;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;

        public event Action? RequestClose;

        public AddReviewViewModel(ReviewService reviewService, UserStore userStore, INavigationService navigationService)
        {
            _reviewService = reviewService;
            _userStore = userStore;
            _navigationService = navigationService;
        }

        public Task OnNavigatedToAsync(Pair pair)
        {
            _currentPair = pair;
            ReviewContent = string.Empty;
            AmountOfHours = 1.0;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task AddReview()
        {
            ValidateAllProperties();

            if (HasErrors || _currentPair == null || _userStore.User == null)
                return;

            var result = await _reviewService.CreateReviewAsync(ReviewContent, _currentPair.Id, _userStore.User.Id, AmountOfHours);
            
            if (result.Success)
            {
                RequestClose?.Invoke();
                await _navigationService.GoBackAsync();
            }
        }
    }
}
