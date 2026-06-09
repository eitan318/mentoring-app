namespace MentoringApp.ViewModel.IService
{
    /// <summary>Abstraction for showing transient toast notifications and confirm dialogs, implemented in the view layer.</summary>
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
