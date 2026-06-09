
namespace MentoringApp.ViewModel.ViewModelHelper
{
    /// <summary>Marks a view model as a navigation target; the navigation service calls these lifecycle hooks on enter/leave.</summary>
    public interface INavigatable
    {
        Task OnNavigatedToAsync()
        {
            return Task.CompletedTask;
        }

        Task OnNavigatedFromAsync()
        {
            return Task.CompletedTask;
        }
    }


    /// <summary>A navigation target that receives a typed parameter when navigated to.</summary>
    public interface INavigatable<TParameter> : INavigatable
    {
        Task OnNavigatedToAsync(TParameter parameter);
    }
}
