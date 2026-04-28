using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.ViewModel.Supervisor;
using MentoringApp.ViewModel.ViewModelHelper;
using System.Collections.ObjectModel;

namespace MentoringApp.ViewModel.ViewModel.Admin;

public partial class ManagePairsViewModel : ObservableObject, INavigatable
{
    private readonly IWindowService _windowService;
    private readonly INavigationService _navigationService;
    private readonly PairApiClient _pairClient;
    private readonly ReviewApiClient _reviewClient;
    private readonly IssueApiClient _issueClient;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    public ObservableCollection<PairModel> AllPairs { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SeparateCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectPairCommand))]
    private PairModel? _selectedPair;

    [ObservableProperty] private int _selectedPairReviewCount;
    [ObservableProperty] private int _selectedPairActiveIssueCount;
    [ObservableProperty] private bool _isLoadingDetails;

    private bool HasSelectedPair => SelectedPair != null;

    [ObservableProperty] private string _searchText = string.Empty;

    public IEnumerable<PairModel> FilteredPairs
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return AllPairs;
            return AllPairs.Where(p =>
                p.Mentor.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Mentee.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
    }

    partial void OnSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredPairs));

    public ManagePairsViewModel(
        IWindowService windowService,
        INavigationService navigationService,
        PairApiClient pairClient,
        ReviewApiClient reviewClient,
        IssueApiClient issueClient,
        IToastService toastService,
        ILocalizationService loc)
    {
        _windowService = windowService;
        _navigationService = navigationService;
        _pairClient = pairClient;
        _reviewClient = reviewClient;
        _issueClient = issueClient;
        _toastService = toastService;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync() => await RefreshPairs();

    private async Task RefreshPairs()
    {
        var pairs = await _pairClient.GetAllAsync();
        AllPairs = new ObservableCollection<PairModel>(pairs);
        SelectedPair = null;
        OnPropertyChanged(nameof(FilteredPairs));
    }

    async partial void OnSelectedPairChanged(PairModel? newSelectedPair)
    {
        if (newSelectedPair == null) return;

        IsLoadingDetails = true;
        SelectedPairReviewCount = 0;
        SelectedPairActiveIssueCount = 0;

        var reviews = await _reviewClient.GetByPairAsync(newSelectedPair.Id);
        SelectedPairReviewCount = reviews.Count();

        var mentorIssues = await _issueClient.GetByUserAsync(newSelectedPair.Mentor.Id);
        var menteeIssues = await _issueClient.GetByUserAsync(newSelectedPair.Mentee.Id);
        SelectedPairActiveIssueCount = mentorIssues.Count(i => !i.IsResolved) + menteeIssues.Count(i => !i.IsResolved);

        IsLoadingDetails = false;
    }

    [RelayCommand] private void CreatePair() => _navigationService.NavigateToAsync<CreatePairViewModel>();

    [RelayCommand(CanExecute = nameof(HasSelectedPair))]
    private async Task SelectPair()
    {
        if (SelectedPair != null)
            await _navigationService.NavigateToAsync<PairDetailsViewModel, int>(SelectedPair.Id);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPair))]
    private async Task Separate()
    {
        if (SelectedPair is null) return;
        if (!await _toastService.ConfirmAsync(_loc.Get("ManagePairs_ConfirmSeparate_Title"), _loc.Get("ManagePairs_ConfirmSeparate_Body"))) return;
        await _pairClient.DeleteAsync(SelectedPair.Id);
        await RefreshPairs();
    }
}