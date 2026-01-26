using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModelHelper
{
    public delegate TViewModel ViewModelFactory<TParameter, TViewModel>(TParameter parameter);


    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly NavigationStore _navigationStore;

        public NavigationService(IServiceProvider serviceProvider, NavigationStore navigationStore)
        {
            _serviceProvider = serviceProvider;
            _navigationStore = navigationStore;
        }

        public Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter) 
            where TViewModel : ViewModelBase, INavigatable<TParameter>
        {
            // ActivatorUtilities still does the heavy lifting of DI
            var viewModel = ActivatorUtilities.CreateInstance<TViewModel>(
                _serviceProvider, 
                new object[] { parameter! }
            );

            _navigationStore.CurrentViewModel = viewModel;
            return Task.CompletedTask;
        }
        public Task NavigateToAsync<TViewModel>() 
            where TViewModel : ViewModelBase
        {
            // No extra parameters passed; ActivatorUtilities gets everything from DI
            TViewModel viewModel = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);

            _navigationStore.CurrentViewModel = viewModel;

            return Task.CompletedTask;
        }

        public Task GoBackAsync()
        {
            if(_navigationStore.CanGoBack())
            {
                _navigationStore.GoBack();
            }
            return Task.CompletedTask;
        }

    }
}


