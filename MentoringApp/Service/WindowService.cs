using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace MentoringApp.Service
{

    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }



        // Generic method to show ANY ViewModel as a window
        public void ShowDialog<TViewModel>(Action<TViewModel>? configure = null) 
            where TViewModel : ViewModelBase
        {
            var vm = _serviceProvider.GetRequiredService<TViewModel>();
            configure?.Invoke(vm);

            var window = new Window
            {
                Content = vm, // WPF finds the View via App.xaml DataTemplates
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            if (vm is ICloseable closeable)
            {
                closeable.RequestClose += () => window.Close();
            }

            window.ShowDialog();
        }
    }
}
