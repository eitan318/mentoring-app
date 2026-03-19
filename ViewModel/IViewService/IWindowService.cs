using MentoringApp.ViewModel.ViewModelHelper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.IService
{
    public interface IWindowService
    {
        Task ShowDialogAsync<TViewModel>()
            where TViewModel : class, INavigatable;

        Task ShowDialogAsync<TViewModel, TParameter>(TParameter parameter)
            where TViewModel : class, INavigatable<TParameter>;
    }
}
