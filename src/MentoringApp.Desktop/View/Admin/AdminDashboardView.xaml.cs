using System.Windows.Controls;

namespace MentoringApp.View.Admin
{
    /// <summary>
    /// Shell view for the admin dashboard — hosts the top navbar and delegates all
    /// content rendering to the active sub-page via ActiveSubPage binding.
    /// </summary>
    public partial class AdminDashboardView : UserControl
    {
        public AdminDashboardView()
        {
            InitializeComponent();
        }
    }
}
