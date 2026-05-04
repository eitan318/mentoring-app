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

        public string SaveFile(string filter, string defaultFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
