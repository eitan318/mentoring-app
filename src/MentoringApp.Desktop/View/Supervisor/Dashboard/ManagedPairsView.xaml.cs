using System.Windows.Controls;
using System.Windows.Input;
using MentoringApp.ViewModel.ViewModel.Supervisor;

namespace MentoringApp.View.Supervisor.Dashboard
{
    public partial class ManagedPairsView : UserControl
    {
        public ManagedPairsView()
        {
            InitializeComponent();
        }

        private void OnPairItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is PairProgressItem selectedItem &&
                DataContext is SupervisorDashboardViewModel vm)
            {
                vm.SelectPairCommand.Execute(selectedItem);
            }
        }
    }
}
