using System;
using System.Globalization;
using System.Windows.Data;
using MentoringApp.Localization;

namespace MentoringApp.Converter
{
    /// <summary>
    /// Converts AssignedClasses.Count to a localized display string.
    ///
    /// Expected MultiBinding values:
    ///   values[0] = int (AssignedClasses.Count)
    ///   values[1] = TranslationSource.CurrentCulture  (triggers re-evaluation on language switch)
    ///
    /// Returns ManageUsers_NoClassesAssigned when count == 0,
    /// otherwise ManageUsers_ClassesAssigned_Format formatted with the count.
    /// </summary>
    public class LocalizedClassCountConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1 || values[0] is not int count)
                return string.Empty;

            if (count == 0)
                return TranslationSource.Instance["ManageUsers_NoClassesAssigned"] ?? "No classes assigned";

            string fmt = TranslationSource.Instance["ManageUsers_ClassesAssigned_Format"] ?? "{0} class(es) assigned";
            return string.Format(fmt, count);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
