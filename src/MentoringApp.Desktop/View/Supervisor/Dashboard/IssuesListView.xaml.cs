using System.Windows.Controls;
using System.Windows.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModel.Supervisor;

namespace MentoringApp.View.Supervisor.Dashboard
{
    public partial class IssuesListView : UserControl
    {
        public IssuesListView()
        {
            InitializeComponent();
        }

        private void OnIssueItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is IssueModel selectedIssue &&
                DataContext is SupervisorDashboardViewModel vm)
            {
                vm.SelectIssueCommand.Execute(selectedIssue);
            }
        }
    }
}
