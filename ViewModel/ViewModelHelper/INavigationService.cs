

using MentoringApp.ViewModel.Store;
using Microsoft.Extensions.DependencyInjection;

namespace MentoringApp.ViewModel.ViewModelHelper
{
    public interface INavigationService
    {
        public Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : ViewModelBase, INavigatable<TParameter>;
        public Task NavigateToAsync<TViewModel>() where TViewModel : ViewModelBase;
        public Task GoBackAsync();
    }

}
