using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MentoringApp.ViewModel.IService;
using System.Collections.ObjectModel;
using System.Windows;

namespace MentoringApp.Service
{
    public enum ToastType { Info, Success, Warning, Error }

    public class ToastItem
    {
        public string Message { get; }
        public ToastType Type { get; }
        public IRelayCommand CloseCommand { get; }

        public ToastItem(string message, ToastType type, Action<ToastItem> close)
        {
            Message = message;
            Type = type;
            CloseCommand = new RelayCommand(() => close(this));
        }
    }

    public class ModalDialogModel
    {
        public string Title { get; }
        public string Message { get; }
        public bool IsConfirm { get; }
        public IRelayCommand OkCommand { get; }
        public IRelayCommand CancelCommand { get; }

        internal ModalDialogModel(string title, string message, bool isConfirm, Action<bool> onResult)
        {
            Title = title;
            Message = message;
            IsConfirm = isConfirm;
            OkCommand = new RelayCommand(() => onResult(true));
            CancelCommand = new RelayCommand(() => onResult(false));
        }
    }

    /// <summary>
    /// Singleton toast/modal service. ViewModels inject <see cref="IToastService"/>;
    /// the MainWindow overlay binds to <see cref="Instance"/> directly via code-behind.
    /// </summary>
    public class ToastService : ObservableObject, IToastService
    {
        public static ToastService Instance { get; } = new();

        public ObservableCollection<ToastItem> Toasts { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasModal))]
        private ModalDialogModel? _activeModal;

        public bool HasModal => ActiveModal != null;

        private ToastService() { }

        public void Info(string message)    => AddToast(message, ToastType.Info);
        public void Success(string message) => AddToast(message, ToastType.Success);
        public void Warning(string message) => AddToast(message, ToastType.Warning);
        public void Error(string message)   => AddToast(message, ToastType.Error);

        private void AddToast(string message, ToastType type)
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                var item = new ToastItem(message, type,
                    t => Application.Current?.Dispatcher.Invoke(() => Toasts.Remove(t)));
                Toasts.Add(item);
                _ = AutoDismissAsync(item);
            });
        }

        private async Task AutoDismissAsync(ToastItem item)
        {
            await Task.Delay(4500);
            Application.Current?.Dispatcher.Invoke(() => Toasts.Remove(item));
        }

        public Task ShowInfoAsync(string title, string message)
            => ShowModalAsync(title, message, isConfirm: false);

        public Task<bool> ConfirmAsync(string title, string message)
            => ShowModalAsync(title, message, isConfirm: true);

        private Task<bool> ShowModalAsync(string title, string message, bool isConfirm)
        {
            var tcs = new TaskCompletionSource<bool>();
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var modal = new ModalDialogModel(title, message, isConfirm, result =>
                {
                    ActiveModal = null;
                    tcs.TrySetResult(result);
                });
                ActiveModal = modal;
            });
            return tcs.Task;
        }
    }
}
