using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MentoringApp.Converter
{
    /// <summary>Visible when value is not null/empty, Collapsed otherwise.</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return Visibility.Collapsed;
            if (value is string s)
                return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
