using MentoringApp.ViewModel.Localization;
using MentoringApp.ViewModel.IService;

namespace MentoringApp.ViewModel.Service
{
    /// <summary>
    /// Implements ILocalizationService by delegating to the singleton TranslationSource.
    /// Registered as a singleton in the DI container.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        public string Get(string key)
            => TranslationSource.Instance[key] ?? key;

        public string Format(string key, params object[] args)
            => string.Format(Get(key), args);
    }
}
