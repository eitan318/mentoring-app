using System.Windows.Controls;
using System.Windows.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModel.Admin;

namespace MentoringApp.View.Admin
{
    /// <summary>
    /// Interaction logic for AdminOverviewView.xaml
    /// </summary>
    public partial class AdminOverviewView : UserControl
    {
        public AdminOverviewView()
        {
            InitializeComponent();
        }

        private void OnForwardedIssueClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item &&
                item.DataContext is IssueModel issue &&
                DataContext is AdminOverviewViewModel vm)
            {
                vm.SelectForwardedIssueCommand.Execute(issue);
            }
        }
    }
}
