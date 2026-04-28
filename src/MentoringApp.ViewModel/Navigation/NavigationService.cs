using MentoringApp.ViewModel.Navigation;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Stack-based navigation service.
///
/// Navigation contexts are layered via <see cref="UseContext"/>: the outermost context
/// drives the MainWindow's content; inner contexts (e.g. the authenticated shell's
/// sub-page area) sit on top of the stack.  Disposing a context handle pops it off and
/// returns control to the caller beneath it.
///
/// Each context owns a <see cref="NavigationStore"/> that maintains its own back-stack,
/// so GoBack() only unwinds within the active context.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    // Parallel stacks — index N of each corresponds to the same context level.
    private readonly Stack<Action<INavigatable>> _contextStack = new();
    private readonly Stack<NavigationStore> _storeStack = new();

    public event Action? NavigationChanged;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers a new navigation context backed by a fresh history stack.
    /// The <paramref name="contextSetter"/> callback is invoked every time the current
    /// ViewModel changes within this context (e.g. to update a ContentControl binding).
    /// Dispose the returned handle to pop the context.
    /// </summary>
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
        NavigationChanged?.Invoke();
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
    public bool CanGoBack() => _storeStack.TryPeek(out var store) && store.CanGoBack();
    public async Task GoBackAsync()
    {
        if (_storeStack.TryPeek(out var store) && store.CanGoBack())
        {
            if (store.CurrentViewModel != null)
            {
                await store.CurrentViewModel.OnNavigatedFromAsync();
            }
            store.GoBack();
            NavigationChanged?.Invoke();
            if (store.CurrentViewModel != null)
            {
                await store.CurrentViewModel.OnNavigatedToAsync();
            }
        }
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
                        // Navigating back to a VM already in history: unwind the stack
                        // up to (but not including) that entry so GoBack() stays correct.
                        while (_navigationHistory.Pop() != value) { }
                    }
                    else
                    {
                        // Forward navigation: push the current VM onto the back-stack.
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