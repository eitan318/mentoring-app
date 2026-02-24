using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.ViewModelPage.Admin
{
    public partial class ManagePairsViewModel : ObservableObject, INavigatable
    {
        private readonly IWindowService _windowService;
        private readonly INavigationService _navigationService;

        [ObservableProperty] private ObservableCollection<Pair> _pairList = new();

        public ManagePairsViewModel(IWindowService windowService, INavigationService navigationService)
        {
            _windowService = windowService;
            _navigationService = navigationService;

            PairList.Add(new Pair());
            PairList.Add(new Pair());
            PairList.Add(new Pair());
            PairList.Add(new Pair());
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
                PairList.Remove(pair);
            }
        }
    }
}