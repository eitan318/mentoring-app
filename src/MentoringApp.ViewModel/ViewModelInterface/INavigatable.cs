
namespace MentoringApp.ViewModel.ViewModelHelper
{
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


    public interface INavigatable<TParameter> : INavigatable 
    {
        Task OnNavigatedToAsync(TParameter parameter);
    }
}
