using System.Globalization;
using System.Windows.Data;

namespace MentoringApp.Converter
{
    /// <summary>Inverts a boolean value (true → false, false → true). Used for IsEnabled bindings.</summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;
    }
}
