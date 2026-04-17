using MentoringApp.Service;
using System.Windows.Controls;

namespace MentoringApp.View.Components
{
    public partial class ToastHostView : UserControl
    {
        public ToastHostView()
        {
            InitializeComponent();
            DataContext = ToastService.Instance;
        }
    }
}
