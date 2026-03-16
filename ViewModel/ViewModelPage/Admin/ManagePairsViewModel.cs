using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.Service;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class ManagePairsViewModel : ObservableObject, INavigatable
    {
        private readonly IWindowService _windowService;
        private readonly INavigationService _navigationService;
        private readonly PairService _pairService;

        [ObservableProperty] private ObservableCollection<Pair> _pairList = new();

        public ManagePairsViewModel(IWindowService windowService, INavigationService navigationService, PairService pairService)
        {
            _windowService = windowService;
            _navigationService = navigationService;
            _pairService = pairService;
            UpdatePairList();
        }

        [RelayCommand]
        private void CreatePair()
        {
            _navigationService.NavigateToAsync<CreatePairViewModel>();

        }

        [RelayCommand]
        private void Separate(Pair? pair)
        {
            if (pair != null)
            {
                _pairService.SeparatePair(pair.Id);
                UpdatePairList();
            }
        }

        public async Task OnNavigatedToAsync()
        {
            UpdatePairList();
        }

        

        private async Task UpdatePairList()
        {
            var res = await _pairService.GetAllPairsAsync();
            PairList = new ObservableCollection<Pair>(res.Data);
        }
    }
}