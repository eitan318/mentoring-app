using System;
using System.Globalization;
using System.Windows.Data;

namespace MentoringApp.Localization
{
    public class DbTranslationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return null;

            var valueFromDb = values[0] as string;
            if (string.IsNullOrEmpty(valueFromDb))
                return valueFromDb;

            // Parameter expects prefix, i.e., "DB_Grade_"
            string prefix = parameter as string ?? "";
            string key = prefix + valueFromDb.Replace(" ", "");

            // Look up the translation structure. If null is returned, fallback to the raw DB English text.
            return TranslationSource.Instance[key] ?? valueFromDb;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
