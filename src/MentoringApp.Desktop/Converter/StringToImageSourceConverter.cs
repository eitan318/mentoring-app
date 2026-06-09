using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MentoringApp.Converter
{
    /// <summary>
    /// Converts a profile-picture path (string) to a WPF BitmapImage.
    /// Handles two formats:
    ///   - Absolute HTTP URL  ("http://...")   — loaded directly from the API
    ///   - Relative URL path  ("uploads/...")  — resolved against <see cref="ApiBaseUrl"/>
    /// Returns null on failure, causing WPF to fall back to the TargetNullValue set in XAML.
    ///
    /// Set <see cref="ApiBaseUrl"/> once at app startup (App.xaml.cs) so every
    /// instance can build full URLs from the relative paths stored in the database.
    /// </summary>
    public class StringToImageSourceConverter : IValueConverter
    {
        /// <summary>
        /// Base address of the API (e.g. "http://localhost:5035").
        /// Set once from App.xaml.cs using the same value read from appsettings.json.
        /// </summary>
        public static string ApiBaseUrl { get; set; } = string.Empty;

        // Shared HttpClient for downloading remote avatars.
        private static readonly System.Net.Http.HttpClient _http = new();
        // Cache decoded images by URL so the converter (called repeatedly by WPF) doesn't re-download.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, BitmapImage> _cache = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                Uri? uri = ResolveUri(path);
                if (uri == null) return null;

                // Remote http(s) image: WPF's BitmapImage downloads asynchronously, so
                // UriSource + Freeze() is unreliable (it freezes an empty image). Instead we
                // download the bytes ourselves and decode from a MemoryStream — fully loaded
                // before Freeze(), so it always renders.
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    return LoadRemote(uri);

                // Local file / pack URI: OnLoad makes EndInit fully load it, so Freeze is safe.
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource   = uri;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage? LoadRemote(Uri uri)
        {
            string key = uri.ToString();
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            // Download on a thread-pool thread to avoid deadlocking the WPF UI thread.
            byte[] bytes = Task.Run(() => _http.GetByteArrayAsync(uri)).GetAwaiter().GetResult();

            using var ms = new System.IO.MemoryStream(bytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption  = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;          // fully-buffered stream → loads synchronously
            bitmap.EndInit();
            bitmap.Freeze();

            _cache[key] = bitmap;
            return bitmap;
        }

        /// <summary>
        /// Resolves a path to an absolute URI:
        ///   - Already absolute HTTP(S) → use as-is.
        ///   - Relative path ("uploads/...") → prepend ApiBaseUrl.
        ///   - Legacy absolute local file path → keep existing behaviour.
        /// Returns null if the path cannot be resolved to a valid URI.
        /// </summary>
        private static Uri? ResolveUri(string path)
        {
            // Already an HTTP(S) URL — load directly
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return new Uri(path, UriKind.Absolute);

            // Relative server path (e.g. "uploads/profile-pictures/42.jpg")
            if (!System.IO.Path.IsPathRooted(path) && !string.IsNullOrEmpty(ApiBaseUrl))
            {
                var baseUrl = ApiBaseUrl.TrimEnd('/');
                var relative = path.TrimStart('/');
                return new Uri($"{baseUrl}/{relative}", UriKind.Absolute);
            }

            // Legacy absolute local file path
            if (System.IO.File.Exists(path))
                return new Uri(path, UriKind.Absolute);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
