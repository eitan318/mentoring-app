using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.IService
{
    /// <summary>Abstraction over native open/save file dialogs, implemented in the view layer to keep WPF out of the view models.</summary>
    public interface IFileService
    {
        string OpenFile(string filter);
        string SaveFile(string filter, string defaultFileName);
    }
}
