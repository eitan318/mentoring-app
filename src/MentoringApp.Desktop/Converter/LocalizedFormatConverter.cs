using MentoringApp.ViewModel.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MentoringApp.Converter
{
    /// <summary>
    /// IMultiValueConverter that looks up a localized format string by key (ConverterParameter),
    /// then substitutes remaining binding values into it via String.Format.
    ///
    /// Expected XAML usage:
    ///   value[0] = first substitution argument
    ///   value[1] = TranslationSource.CurrentCulture (triggers re-evaluation on language switch)
    ///   ConverterParameter = resource key (e.g. "Supervisor_RandomlyMatched_Warning")
    ///
    /// The format string is retrieved from TranslationSource[key] and String.Format is called
    /// with value[0]. Falls back to the raw parameter key if no translation is found.
    /// </summary>
    public class LocalizedFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1 || parameter is not string key)
                return string.Empty;

            string? format = TranslationSource.Instance[key];
            if (string.IsNullOrEmpty(format))
                return key;

            try
            {
                var args = new object[values.Length - 1];
                for (int i = 0; i < args.Length; i++)
                    args[i] = values[i];
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
