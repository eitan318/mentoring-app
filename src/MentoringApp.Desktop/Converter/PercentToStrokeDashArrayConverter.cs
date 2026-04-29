using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MentoringApp.Converter
{
    public class PercentToStrokeDashArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = value is double d ? d : 0;

            double diameter = 100, thickness = 10;
            if (parameter is string s)
            {
                var parts = s.Split(',');
                if (parts.Length == 2)
                {
                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out diameter);
                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out thickness);
                }
            }

            double circumferenceUnits = Math.PI * diameter / thickness;
            double dash = Math.Clamp(percent / 100.0, 0, 1) * circumferenceUnits;
            double gap = circumferenceUnits * 2;

            return new DoubleCollection { dash, gap };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
