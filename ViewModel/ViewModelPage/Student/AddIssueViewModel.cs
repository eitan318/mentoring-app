using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.Student
{
    public partial class AddIssueViewModel : ObservableValidator, INavigatable<IEnumerable<IssueCategoryModel>>, ICloseable 
    {
        // Data for the View
        [ObservableProperty] private ObservableCollection<IssueCategoryModel> _issueCategoryList;

        [ObservableProperty] 
        [Required(ErrorMessage = "You must select a category")]
        private IssueCategoryModel? _selectedIssueCategory;
        
        [ObservableProperty] 
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Please describe the issue.")]
        [MinLength(5, ErrorMessage = "Description is too short.")]
        private string _issueDescription = string.Empty;

        private readonly IssueService _issueService;
        private readonly INavigationService _navigationService;
        private readonly UserStore _userStore;

        public event Action? RequestClose;

        public AddIssueViewModel(IssueService issueService, UserStore userStore, INavigationService navigationService)
        {
            _issueService = issueService;
            _userStore = userStore;
            _navigationService = navigationService;
            _issueCategoryList = [];
        }
    
        public Task OnNavigatedToAsync(IEnumerable<IssueCategoryModel> categories)
        {
            IssueCategoryList = [.. categories];
            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task AddIssue()
        {
            ValidateAllProperties();

            if (HasErrors || SelectedIssueCategory == null)
                return;

            if (_userStore.User != null)
            {
                await _issueService.CreateIssueAsync(IssueDescription, SelectedIssueCategory.Id, _userStore.User.Id);
            }

            RequestClose?.Invoke();
            _navigationService.GoBackAsync();
        }
    }
}