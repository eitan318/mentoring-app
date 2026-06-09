using System.Windows;
using System.Windows.Controls;

namespace MentoringApp.View.Admin.Overview
{
    public partial class StepCircleControl : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(StepCircleControl), new PropertyMetadata(false));

        public static readonly DependencyProperty IsCompleteProperty =
            DependencyProperty.Register(nameof(IsComplete), typeof(bool), typeof(StepCircleControl), new PropertyMetadata(false));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public bool IsComplete
        {
            get => (bool)GetValue(IsCompleteProperty);
            set => SetValue(IsCompleteProperty, value);
        }

        public StepCircleControl()
        {
            InitializeComponent();
        }
    }
}
