using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<Action<INavigatable>> _contextStack = new();
    private readonly Stack<NavigationStore> _storeStack = new();

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDisposable UseContext(Action<INavigatable> contextSetter)
    {
        var store = new NavigationStore();

        // When the store's VM changes, push that change to the UI context (MainWindow, etc.)
        Action updateUi = () => contextSetter(store.CurrentViewModel);
        store.CurrentViewModelChanged += updateUi;

        _contextStack.Push(contextSetter);
        _storeStack.Push(store);

        // Return a disposer that cleans up the stacks and events
        return new ContextReliever(() =>
        {
            store.CurrentViewModelChanged -= updateUi;
            _contextStack.Pop();
            _storeStack.Pop();
        });
    }

    private async Task NavigateCoreAsync<TViewModel>(TViewModel vm, Func<Task> onNavigatedTo)
        where TViewModel : class, INavigatable
    {
        if (!_storeStack.TryPeek(out var currentStore)) return;

        if (currentStore.CurrentViewModel != null)
        {
            await currentStore.CurrentViewModel.OnNavigatedFromAsync();
        }

        currentStore.CurrentViewModel = vm;
        await onNavigatedTo();
    }

    public async Task NavigateToAsync<TViewModel>() where TViewModel : class, INavigatable
    {
        var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
        await NavigateCoreAsync(vm, () => vm.OnNavigatedToAsync());
    }

    public async Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
        where TViewModel : class, INavigatable<TParameter>
    {
        var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
        await NavigateCoreAsync(vm, () => vm.OnNavigatedToAsync(parameter));
    }

    public Task GoBackAsync()
    {
        if (_storeStack.TryPeek(out var store))
        {
            store.GoBack();
        }
        return Task.CompletedTask;
    }
    private class ContextReliever : IDisposable
    {
        private readonly Action _onDispose;
        public ContextReliever(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }

    private class NavigationStore : StoreBase
    {
        private readonly Stack<INavigatable> _navigationHistory = new();
        private INavigatable _currentViewModel;
        public bool CanGoBack() => _navigationHistory.Count > 0;
        public void GoBack()
        {
            if (CanGoBack())
            {
                CurrentViewModel = _navigationHistory.Peek();
            }
        }

        public INavigatable CurrentViewModel
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
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
                CurrentViewModelChanged?.Invoke();
            }
        }

        public event Action CurrentViewModelChanged;

    }
}