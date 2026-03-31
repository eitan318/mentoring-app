using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.ViewModel.IService
{
    public interface IFileService
    {
        string OpenFile(string filter);
        string SaveFile(string filter, string defaultFileName);
    }
}
