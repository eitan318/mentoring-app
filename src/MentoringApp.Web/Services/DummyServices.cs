using System.Threading.Tasks;
using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;

namespace MentoringApp.Web.Services
{
    public class DummyWindowService : IWindowService
    {
        public Task ShowDialogAsync<TViewModel>() where TViewModel : class, INavigatable => Task.CompletedTask;
        public Task ShowDialogAsync<TViewModel, TParameter>(TParameter parameter) where TViewModel : class, INavigatable<TParameter> => Task.CompletedTask;
        public void ShowMessage(string message, string title) { }
        public Task<bool> ShowConfirmAsync(string message, string title) => Task.FromResult(true);
    }

    public class DummyToastService : IToastService
    {
        public void Info(string message) { }
        public void Success(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(true);
    }

    public class DummyFileService : IFileService
    {
        public string OpenFile(string filter) => string.Empty;
        public string SaveFile(string filter, string defaultFileName) => string.Empty;
    }
}
