using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModelPage.Student
{
    public partial class AddIssueViewModel : ObservableValidator, INavigatable<IEnumerable<IssueCategory>>, ICloseable 
    {
        // Data for the View
        [ObservableProperty] private ObservableCollection<IssueCategory> _issueCategoryList;

        [ObservableProperty] 
        [Required(ErrorMessage = "You must select a category")]
        private IssueCategory? _selectedIssueCategory;
        
        [ObservableProperty] 
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Please describe the issue.")]
        [MinLength(5, ErrorMessage = "Description is too short.")]
        private string _issueDescription = string.Empty;


        public event Action? RequestClose;

        public AddIssueViewModel()
        {
            _issueCategoryList = [];
        }
    
        public Task OnNavigatedToAsync(IEnumerable<IssueCategory> categories)
        {
            IssueCategoryList = [.. categories];
            return Task.CompletedTask;
        }

        

        [RelayCommand]
        private void AddIssue()
        {
            ValidateAllProperties();

            if (HasErrors || SelectedIssueCategory == null)
                return;

            Issue issue = new Issue
            {
                Description = IssueDescription,
                Category = SelectedIssueCategory,
                CreationDate = DateTime.Now,
                Id = -1
            };

            RequestClose?.Invoke();
        }
    }
}