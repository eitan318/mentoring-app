using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MentoringApp.Behaviors
{
    /// <summary>
    /// Attached behavior that fires an ICommand when a ListView item is clicked,
    /// passing the clicked item's data context as the parameter.
    /// Clicks that originate from a Button inside the item are ignored so inner
    /// buttons keep their own command behavior.
    /// </summary>
    public static class ListItemSelector
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(ListItemSelector),
                new PropertyMetadata(null, OnCommandChanged));

        public static ICommand? GetCommand(DependencyObject obj) => (ICommand?)obj.GetValue(CommandProperty);
        public static void SetCommand(DependencyObject obj, ICommand? value) => obj.SetValue(CommandProperty, value);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ListView lv) return;
            lv.PreviewMouseLeftButtonDown -= OnPreviewClick;
            if (e.NewValue is ICommand) lv.PreviewMouseLeftButtonDown += OnPreviewClick;
        }

        private static void OnPreviewClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView lv) return;
            var node = e.OriginalSource as DependencyObject;
            while (node != null && node != lv)
            {
                if (node is Button) return;
                if (node is ListViewItem item)
                {
                    var cmd = GetCommand(lv);
                    if (cmd?.CanExecute(item.Content) == true)
                        cmd.Execute(item.Content);
                    return;
                }
                node = VisualTreeHelper.GetParent(node);
            }
        }
    }
}
