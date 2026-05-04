using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.ViewModel.Navigation
{
    public interface INavigationService
    {
        /// <summary>Raised whenever CanGoBack may have changed, allowing reactive UI updates.</summary>
        event Action? CanGoBackChanged;

        Task NavigateToAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : class, INavigatable<TParameter>;

        Task NavigateToAsync<TViewModel>()
            where TViewModel : class, INavigatable;

        /// <summary>Navigate and clear the back-stack (use for sidebar root-level switches).</summary>
        Task NavigateToRootAsync<TViewModel>()
            where TViewModel : class, INavigatable;

        /// <summary>Navigate with a parameter and clear the back-stack.</summary>
        Task NavigateToRootAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : class, INavigatable<TParameter>;

        IDisposable UseContext(Action<INavigatable> contextSetter);
        Task GoBackAsync();
        bool CanGoBack();
    }
}
