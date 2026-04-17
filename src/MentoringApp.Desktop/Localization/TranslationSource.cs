using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MentoringApp.Localization
{
    /// <summary>
    /// Singleton wrapper around the .resx ResourceManager.
    /// Implements INotifyPropertyChanged so that WPF bindings update
    /// instantly when the culture is switched (no page reload needed).
    /// 
    /// Usage in XAML:
    ///   xmlns:loc="clr-namespace:MentoringApp.Localization"
    ///   Content="{Binding [Profile_Username_Label], Source={x:Static loc:TranslationSource.Instance}}"
    /// </summary>
    public class TranslationSource : INotifyPropertyChanged
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        private static readonly Lazy<TranslationSource> _instance =
            new(() => new TranslationSource());

        public static TranslationSource Instance => _instance.Value;

        // ── ResourceManager ──────────────────────────────────────────────────────
        private readonly ResourceManager _resourceManager =
            new ResourceManager("MentoringApp.Localization.Strings",
                                typeof(TranslationSource).Assembly);

        // ── Culture ──────────────────────────────────────────────────────────────
        private CultureInfo _currentCulture = CultureInfo.GetCultureInfo("en");

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture.Equals(value)) return;
                _currentCulture = value;
                // Notify that all indexed bindings should refresh
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }

        // ── Convenience code string ──────────────────────────────────────────────
        /// <summary>Language code, e.g. "en" or "he".</summary>
        public string LanguageCode
        {
            get => _currentCulture.TwoLetterISOLanguageName;
            set => CurrentCulture = CultureInfo.GetCultureInfo(value);
        }

        // ── Indexer — used in XAML bindings ─────────────────────────────────────
        public string? this[string key] =>
            _resourceManager.GetString(key, _currentCulture) ?? $"[{key}]";

        // ── INotifyPropertyChanged ───────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        // ── FlowDirection helper ──────────────────────────────────────────────────
        /// <summary>
        /// Returns RightToLeft for RTL languages (e.g. Hebrew), LeftToRight otherwise.
        /// Bind the Window / root panel's FlowDirection to this.
        /// </summary>
        public System.Windows.FlowDirection FlowDirection =>
            _currentCulture.TextInfo.IsRightToLeft
                ? System.Windows.FlowDirection.RightToLeft
                : System.Windows.FlowDirection.LeftToRight;

        private TranslationSource() { }
    }
}
