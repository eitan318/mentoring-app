
using MentoringApp.Model;
using MentoringApp.ViewModel.ViewModel.Admin;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MentoringApp.View.Admin
{
    public partial class SupervisorAssignmentView : UserControl
    {
        private SystemSettingsViewModel ViewModel => (SystemSettingsViewModel)DataContext;

        private SchoolClassModel? _draggingClass;
        private SupervisorSlot? _dragFromSlot;
        private Point _dragStartPoint;

        public SupervisorAssignmentView()
        {
            InitializeComponent();
        }

        private void ClassChip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ClassChip_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;
            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance) return;

            var element = (FrameworkElement)sender;
            if (element.DataContext is not SchoolClassModel cls) return;

            _draggingClass = cls;
            _dragFromSlot = FindSupervisorSlotInTree(element);

            DragDrop.DoDragDrop(element, cls, DragDropEffects.Move);
        }

        private void SupervisorCard_Drop(object sender, DragEventArgs e)
        {
            if (_draggingClass == null) return;
            var border = (FrameworkElement)sender;
            if (border.DataContext is not SupervisorSlot targetSlot) return;

            RestoreDropZone(border);
            _ = ViewModel.MoveClassAsync(_draggingClass, _dragFromSlot, targetSlot);
            _draggingClass = null;
            _dragFromSlot = null;
        }

        private void PoolDropZone_Drop(object sender, DragEventArgs e)
        {
            if (_draggingClass == null || _dragFromSlot == null) return;

            var border = (FrameworkElement)sender;
            RestoreDropZone(border);
            _ = ViewModel.MoveClassToPoolAsync(_draggingClass, _dragFromSlot);
            _draggingClass = null;
            _dragFromSlot = null;
        }

        private void DropZone_DragEnter(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement el)
                HighlightDropZone(el);
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void DropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void DropZone_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is not FrameworkElement el) return;
            var pos = e.GetPosition(el);
            if (pos.X < 0 || pos.X > el.ActualWidth || pos.Y < 0 || pos.Y > el.ActualHeight)
                RestoreDropZone(el);
        }

        private static void HighlightDropZone(FrameworkElement el)
        {
            if (el is Border b)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromRgb(0x42, 0x85, 0xF4));
                b.BorderThickness = new Thickness(2);
                b.Background = new SolidColorBrush(Color.FromArgb(0x18, 0x42, 0x85, 0xF4));
            }
        }

        private static void RestoreDropZone(FrameworkElement el)
        {
            if (el is Border b)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
                b.BorderThickness = new Thickness(1);
                b.Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));
            }
        }

        private static SupervisorSlot? FindSupervisorSlotInTree(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is FrameworkElement fe && fe.DataContext is SupervisorSlot slot)
                    return slot;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
