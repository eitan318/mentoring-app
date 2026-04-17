
using System.Windows.Controls;
using System.Windows.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModel.Admin;

namespace MentoringApp.View.Admin
{
    /// <summary>
    /// Interaction logic for AdminDashboard.xaml
    /// </summary>
    public partial class AdminDashboardView : UserControl
    {
        public AdminDashboardView()
        {
            InitializeComponent();
        }

        private void OnForwardedIssueClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item &&
                item.DataContext is IssueModel issue &&
                DataContext is AdminDashboardViewModel vm)
            {
                vm.SelectForwardedIssueCommand.Execute(issue);
            }
        }
    }
}
