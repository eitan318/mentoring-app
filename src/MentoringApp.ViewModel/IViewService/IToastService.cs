namespace MentoringApp.ViewModel.IService
{
    public interface IToastService
    {
        void Info(string message);
        void Success(string message);
        void Warning(string message);
        void Error(string message);
        Task ShowInfoAsync(string title, string message);
        Task<bool> ConfirmAsync(string title, string message);
    }
}
