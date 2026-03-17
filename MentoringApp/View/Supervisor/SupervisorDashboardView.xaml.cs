
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelPage.Supervisor;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
namespace MentoringApp.View.Supervisor
{
    /// <summary>
    /// Interaction logic for Supervisorview.xaml
    /// </summary>
    public partial class SupervisorDashboardView : UserControl
    {
        public SupervisorDashboardView()
        {
            InitializeComponent();
        }

        private void OnIssueItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Issue selectedIssue)
            {
                if (DataContext is SupervisorDashboardViewModel vm)
                {
                    // Directly call the command on the VM
                    vm.SelectIssueCommand.Execute(selectedIssue);
                }
            }
        }

       

    }

}
