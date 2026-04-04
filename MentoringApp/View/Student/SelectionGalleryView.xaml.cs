using MentoringApp.ViewModel.ViewModelPage.Student;
using System.Windows.Controls;

namespace MentoringApp.View.Student
{
    public partial class SelectionGalleryView : UserControl
    {
        public SelectionGalleryView()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                if (DataContext is SelectionGalleryViewModel vm)
                    await vm.LoadAsync();
            };
        }
    }
}
