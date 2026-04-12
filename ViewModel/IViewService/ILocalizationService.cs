namespace MentoringApp.ViewModel.IService
{
    /// <summary>
    /// Abstraction over the localization/translation system.
    /// Implemented in the view layer using TranslationSource.
    /// This keeps the ViewModel project free of any WPF/view dependency.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>Returns the localized string for the given resource key.</summary>
        string Get(string key);

        /// <summary>Returns a localized format string with the supplied arguments applied.</summary>
        string Format(string key, params object[] args);
    }
}
