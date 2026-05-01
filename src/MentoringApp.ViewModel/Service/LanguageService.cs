using MentoringApp.ViewModel.Localization;
using MentoringApp.ViewModel.IService;

namespace MentoringApp.ViewModel.Service
{
    /// <summary>
    /// Implements ILanguageService by delegating to the singleton TranslationSource.
    /// Registered as a singleton in the DI container so ViewModels can freely call it.
    /// </summary>
    public class LanguageService : ILanguageService
    {
        public string CurrentLanguage => TranslationSource.Instance.LanguageCode;

        public void ApplyLanguage(string languageCode)
        {
            TranslationSource.Instance.LanguageCode = languageCode;
        }
    }
}
