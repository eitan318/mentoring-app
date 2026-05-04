using MentoringApp.ViewModel.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MentoringApp.Converter
{
    /// <summary>
    /// IMultiValueConverter that translates DB-stored English display names into the
    /// active UI language using the <see cref="TranslationSource"/> resource dictionary.
    ///
    /// Expected XAML usage:
    ///   value[0] = raw DB string (e.g. "9th")
    ///   value[1] = TranslationSource.CurrentCulture (used only to trigger re-evaluation on language switch)
    ///   ConverterParameter = resource key prefix (e.g. "DB_Grade_")
    ///
    /// The converter concatenates prefix + value (spaces stripped) to form the lookup key
    /// (e.g. "DB_Grade_9th"), then falls back to the raw DB text if no translation is found.
    /// </summary>
    public class DbTranslationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return null;

            var valueFromDb = values[0]?.ToString();
            if (string.IsNullOrEmpty(valueFromDb))
                return valueFromDb;

            // ConverterParameter is the key prefix, e.g. "DB_Grade_"
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
