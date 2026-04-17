using System;
using System.Globalization;
using System.Windows.Data;
using MentoringApp.Localization;

namespace MentoringApp.Converter
{
    /// <summary>
    /// Combines a raw DB grade name and a class number into a single localized display string.
    ///
    /// Expected MultiBinding values:
    ///   values[0] = raw DB grade name string (e.g. "10th")
    ///   values[1] = class number (int)
    ///   values[2] = TranslationSource.CurrentCulture  (triggers re-evaluation on language switch)
    ///
    /// Uses DB_Grade_{name} to localize the grade, then applies GradeClass_Format.
    /// English result: "10th Grade, Class 4"
    /// Hebrew result:  "כיתה י' 4"
    /// </summary>
    public class GradeClassDisplayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return string.Empty;

            var rawGrade = values[0] as string;
            if (string.IsNullOrEmpty(rawGrade)) return string.Empty;
            if (values[1] is not int classNum) return string.Empty;

            string localizedGrade = TranslationSource.Instance["DB_Grade_" + rawGrade.Replace(" ", "")]
                                    ?? rawGrade;

            string fmt = TranslationSource.Instance["GradeClass_Format"] ?? "{0}, Class {1}";
            return string.Format(fmt, localizedGrade, classNum);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
