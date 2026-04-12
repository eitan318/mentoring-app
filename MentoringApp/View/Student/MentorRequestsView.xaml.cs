using MentoringApp.ViewModel.ViewModel.Student;
using System.Windows.Controls;

namespace MentoringApp.View.Student
{
    public partial class MentorRequestsView : UserControl
    {
        public MentorRequestsView()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                if (DataContext is MentorRequestsViewModel vm)
                    await vm.LoadAsync();
            };
        }
    } 
}
