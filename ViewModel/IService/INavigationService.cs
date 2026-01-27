using MentoringApp.ViewModel.Store;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MentoringApp.ViewModel.IService
{
    public interface INavigationService
    {
        Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : class, INavigatable<TParameter>;

        Task NavigateToAsync<TViewModel>()
            where TViewModel : class, INavigatable;

        public Task GoBackAsync();
    }

}
