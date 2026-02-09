
using MentoringApp.Model;
using System.Windows.Controls;
using System.Windows.Data;
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

        private void FilterPending(object sender, FilterEventArgs e) => e.Accepted = e.Item is Issue issue && !issue.IsResolved;

        private void FilterResolved(object sender, FilterEventArgs e) => e.Accepted = e.Item is Issue issue && issue.IsResolved;

    }

}
