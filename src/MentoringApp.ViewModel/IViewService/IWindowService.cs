using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.IService
{
    /// <summary>Abstraction for opening modal dialog windows (by view model) and message/confirm boxes, implemented in the view layer.</summary>
    public interface IWindowService
    {
        Task ShowDialogAsync<TViewModel>()
            where TViewModel : class, INavigatable;

        Task ShowDialogAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : class, INavigatable<TParameter>;

        void ShowMessage(string message, string title);
        Task<bool> ShowConfirmAsync(string message, string title);
    }
}
