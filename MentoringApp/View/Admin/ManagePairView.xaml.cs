using System;
using System.Windows.Controls;
using System.Windows.Input;
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModelPage.Admin;

namespace MentoringApp.View.Admin
{
    /// <summary>
    /// Interaction logic for ManagePairViewModel.xaml
    /// </summary>
    public partial class ManagePairViewModel : UserControl
    {
        public ManagePairViewModel()
        {
            InitializeComponent();
        }

        private void OnPairItemClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.Content is Pair selectedPair)
            {
                if (DataContext is ManagePairsViewModel vm)
                {
                    vm.SelectPairCommand.Execute(selectedPair);
                }
            }
        }
    }
}
