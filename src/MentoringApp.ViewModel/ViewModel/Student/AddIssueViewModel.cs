using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.Model;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MentoringApp.ViewModel.ViewModel.Student;

public partial class AddIssueViewModel : ObservableValidator, INavigatable<IEnumerable<IssueCategoryModel>>, ICloseable
{
    [ObservableProperty] private ObservableCollection<IssueCategoryModel> _issueCategoryList = [];

    [ObservableProperty]
    [Required(ErrorMessage = "You must select a category")]
    private IssueCategoryModel? _selectedIssueCategory;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Please describe the issue.")]
    [MinLength(5, ErrorMessage = "Description is too short.")]
    private string _issueDescription = string.Empty;

    private readonly IssueApiClient _issueClient;
    private readonly INavigationService _navigationService;
    private readonly UserStore _userStore;

    public event Action? RequestClose;

    public AddIssueViewModel(IssueApiClient issueClient, UserStore userStore, INavigationService navigationService)
    {
        _issueClient = issueClient;
        _userStore = userStore;
        _navigationService = navigationService;
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
        if (HasErrors || SelectedIssueCategory == null || _userStore.User == null) return;

        await _issueClient.CreateAsync(new CreateIssueRequest(IssueDescription, SelectedIssueCategory.Id, _userStore.User.Id));
        RequestClose?.Invoke();
        _navigationService.GoBackAsync();
    }
}
