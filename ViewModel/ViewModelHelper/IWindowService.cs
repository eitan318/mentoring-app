using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModelHelper
{
    public interface IWindowService
    {
        void ShowDialog<TViewModel>(Action<TViewModel>? configure = null)
            where TViewModel : ViewModelBase;
    }
}
