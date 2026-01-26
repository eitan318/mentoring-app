using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.Store
{
 public class NavigationStore : StoreBase
    {
        private readonly Stack<ViewModelBase> _navigationHistory = new();
        private ViewModelBase _currentViewModel;

        public bool CanGoBack() => _navigationHistory.Count > 0;

        public void GoBack()
        {
            if (CanGoBack())
            {
                CurrentViewModel = _navigationHistory.Peek();
            }
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel != null)
                {
                    if (_navigationHistory.Contains(value))
                    {
                        while (_navigationHistory.Pop() != value) { }
                    }
                    else
                    {
                        _navigationHistory.Push(_currentViewModel);
                    }
                }

                _currentViewModel?.OnNavigatedFrom();
                _currentViewModel = value;
                _currentViewModel?.OnNavigatedTo();

                OnPropertyChanged(nameof(CurrentViewModel));
                CurrentViewModelChanged?.Invoke();
            }
        }
        
        public event Action CurrentViewModelChanged;
    }
}
