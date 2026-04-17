namespace MentoringApp.ViewModel.IService
{
    /// <summary>
    /// Abstraction over the UI language/culture system.
    /// Implemented in the view layer using TranslationSource.
    /// This keeps the ViewModel project free of any WPF/view dependency.
    /// </summary>
    public interface ILanguageService
    {
        /// <summary>
        /// Gets the current language code (e.g. "en" or "he").
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// Applies the given language code to the UI immediately.
        /// </summary>
        void ApplyLanguage(string languageCode);
    }
}
