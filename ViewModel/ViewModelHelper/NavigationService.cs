using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NavigationStore _navigationStore;

    public NavigationService(IServiceProvider serviceProvider, NavigationStore navigationStore)
    {
        _serviceProvider = serviceProvider;
        _navigationStore = navigationStore;
    }

    private async Task NavigateCoreAsync<TViewModel>(TViewModel vm, Func<Task> onNavigatedTo)
        where TViewModel : class, INavigatable
    {
        if (_navigationStore.CurrentViewModel is INavigatable oldVm)
        {
            await oldVm.OnNavigatedFromAsync();
        }

        await onNavigatedTo();
        _navigationStore.CurrentViewModel = vm;
    }

    public async Task NavigateToAsync<TViewModel>()
        where TViewModel : class, INavigatable
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
        _navigationStore.GoBack();
        return Task.CompletedTask;
    }
}