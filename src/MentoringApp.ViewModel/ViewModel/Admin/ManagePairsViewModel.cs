using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ApiClient.Clients;
using MentoringApp.ApiClient.Models;
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
    private readonly UserApiClient _userClient;
    private readonly IToastService _toastService;
    private readonly ILocalizationService _loc;

    // User name cache for display
    private Dictionary<int, string> _userNames = new();

    public ObservableCollection<PairDisplayItem> AllPairs { get; set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SeparateCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectPairCommand))]
    private PairDisplayItem? _selectedPair;

    [ObservableProperty] private int _selectedPairReviewCount;
    [ObservableProperty] private int _selectedPairActiveIssueCount;
    [ObservableProperty] private bool _isLoadingDetails;

    private bool HasSelectedPair => SelectedPair != null;

    [ObservableProperty] private string _searchText = string.Empty;

    public IEnumerable<PairDisplayItem> FilteredPairs
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return AllPairs;
            return AllPairs.Where(p =>
                p.MentorName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.MenteeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
    }

    partial void OnSearchTextChanged(string v) => OnPropertyChanged(nameof(FilteredPairs));

    public ManagePairsViewModel(
        IWindowService windowService,
        INavigationService navigationService,
        PairApiClient pairClient,
        ReviewApiClient reviewClient,
        IssueApiClient issueClient,
        UserApiClient userClient,
        IToastService toastService,
        ILocalizationService loc)
    {
        _windowService = windowService;
        _navigationService = navigationService;
        _pairClient = pairClient;
        _reviewClient = reviewClient;
        _issueClient = issueClient;
        _userClient = userClient;
        _toastService = toastService;
        _loc = loc;
    }

    public async Task OnNavigatedToAsync() => await RefreshPairs();

    private async Task RefreshPairs()
    {
        var users = await _userClient.GetAllAsync();
        _userNames = users.ToDictionary(u => u.Id, u => u.UserName);

        var pairs = await _pairClient.GetAllAsync();
        AllPairs = new ObservableCollection<PairDisplayItem>(pairs.Select(p => new PairDisplayItem(p, _userNames)));
        SelectedPair = null;
        OnPropertyChanged(nameof(FilteredPairs));
    }

    async partial void OnSelectedPairChanged(PairDisplayItem? value)
    {
        if (value == null) return;

        IsLoadingDetails = true;
        SelectedPairReviewCount = 0;
        SelectedPairActiveIssueCount = 0;

        var reviews = await _reviewClient.GetByPairAsync(value.Id);
        SelectedPairReviewCount = reviews.Count();

        var mentorIssues = await _issueClient.GetByUserAsync(value.MentorId);
        var menteeIssues = await _issueClient.GetByUserAsync(value.MenteeId);
        SelectedPairActiveIssueCount = mentorIssues.Count(i => !i.IsResolvedBool) + menteeIssues.Count(i => !i.IsResolvedBool);

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

public class PairDisplayItem
{
    public PairResponse Pair { get; }
    public int Id => Pair.Id;
    public int MentorId => Pair.MentorId;
    public int MenteeId => Pair.MenteeId;
    public string MentorName { get; }
    public string MenteeName { get; }

    public PairDisplayItem(PairResponse pair, Dictionary<int, string> userNames)
    {
        Pair = pair;
        userNames.TryGetValue(pair.MentorId, out var mn); MentorName = mn ?? $"User {pair.MentorId}";
        userNames.TryGetValue(pair.MenteeId, out var men); MenteeName = men ?? $"User {pair.MenteeId}";
    }
}
