using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MentoringApp.View.Admin
{
    /// <summary>
    /// Shell view for the admin dashboard — hosts the side navigation and delegates
    /// content rendering to the active sub-page via ActiveSubPage binding.
    ///
    /// Side-nav behavior: on narrow windows the nav becomes a 56px icon-only "rail";
    /// hovering the rail expands it to the full 200px so labels are visible.
    /// </summary>
    public partial class AdminDashboardView : UserControl
    {
        // Window width below which the side-nav switches to rail mode.
        private const double CompactBreakpoint = 1000;
        private const double RailWidth = 70;
        private const double FullWidth = 200;

        // True iff the side-nav is currently collapsed to icon-only mode.
        // XAML labels bind their visibility to this via RelativeSource AncestorType=UserControl.
        public static readonly DependencyProperty IsRailModeProperty =
            DependencyProperty.Register(nameof(IsRailMode), typeof(bool), typeof(AdminDashboardView),
                new PropertyMetadata(false));

        public bool IsRailMode
        {
            get => (bool)GetValue(IsRailModeProperty);
            private set => SetValue(IsRailModeProperty, value);
        }

        private bool _isCompactMode;
        private bool _hovered;

        public AdminDashboardView()
        {
            InitializeComponent();
            Loaded += (_, _) => Apply();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _isCompactMode = e.NewSize.Width < CompactBreakpoint;
            Apply();
        }

        private void OnSideNavMouseEnter(object sender, MouseEventArgs e)
        {
            _hovered = true;
            Apply();
        }

        private void OnSideNavMouseLeave(object sender, MouseEventArgs e)
        {
            _hovered = false;
            Apply();
        }

        private void Apply()
        {
            if (SideNavColumn == null) return;
            bool rail = _isCompactMode && !_hovered;
            SideNavColumn.Width = new GridLength(rail ? RailWidth : FullWidth);
            IsRailMode = rail;
        }
    }
}
