using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.ViewModelHelper
{
    /// <summary>Implemented by dialog view models that need to ask their host window to close (via <see cref="RequestClose"/>).</summary>
    public interface ICloseable
    {
        event Action? RequestClose;
    }

}
