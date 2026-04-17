using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.Navigation
{
    public interface INavigationService
    {
        Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : class, INavigatable<TParameter>;

        Task NavigateToAsync<TViewModel>()
            where TViewModel : class, INavigatable;

        IDisposable UseContext(Action<INavigatable> contextSetter);
        Task GoBackAsync();
        bool CanGoBack();
    }

}
