using MentoringApp.ViewModel.IService;
using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace MentoringApp.Service
{

    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IToastService _toastService;

        public WindowService(IServiceProvider serviceProvider, IToastService toastService)
        {
            _serviceProvider = serviceProvider;
            _toastService = toastService;
        }

        public async Task ShowDialogAsync<TViewModel>() 
            where TViewModel : class, INavigatable
        {
            var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
            await vm.OnNavigatedToAsync();
            OpenWindow(vm);
        }

        public async Task ShowDialogAsync<TViewModel, TParameter>(TParameter parameter) 
            where TViewModel : class, INavigatable<TParameter>
        {
            var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
            await vm.OnNavigatedToAsync(parameter);
            OpenWindow(vm);
        }

        public void ShowMessage(string message, string title)
        {
            _toastService.Info(message);
        }

        public async Task<bool> ShowConfirmAsync(string message, string title)
        {
            return await _toastService.ConfirmAsync(title, message);
        }

        private static void OpenWindow(INavigatable vm)
        {
            var window = new Window
            {
                Content = vm, // WPF will look for DataTemplate
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            if (vm is ICloseable closeable)
            {
                closeable.RequestClose += async () => 
                {
                    await vm.OnNavigatedFromAsync();
                    window.Close();
                };
            }
            window.ShowDialog();
        }
    }
}
