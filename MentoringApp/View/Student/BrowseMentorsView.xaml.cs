using MentoringApp.ViewModel.ViewModelPage.Student;
using System.Windows.Controls;

namespace MentoringApp.View.Student
{
    public partial class BrowseMentorsView : UserControl
    {
        public BrowseMentorsView()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                if (DataContext is BrowseMentorsViewModel vm)
                    await vm.LoadAsync();
            };
        }
    }
}
