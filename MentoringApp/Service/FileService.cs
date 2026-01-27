using MentoringApp.ViewModel.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Service
{
    public class FileService : IFileService
    {
        public string OpenFile(string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
