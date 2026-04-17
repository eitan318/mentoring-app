using System.Text.Json;

namespace MentoringApp.Service
{
    /// <summary>
    /// Persists a lightweight session file so the user is not prompted to log in
    /// on every launch.  The file is written to the app's LocalApplicationData
    /// folder and is therefore specific to the Windows user account on this device.
    ///
    /// Session lifetime: indefinite — cleared only by an explicit <see cref="ClearSession"/> call
    /// (i.e. the user clicked Logout).
    /// </summary>
    public class SessionService
    {
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MentoringApp", "session.json");

        public void SaveSession(int userId)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SessionFilePath)!);
            var payload = new SessionPayload { UserId = userId };
            File.WriteAllText(SessionFilePath, JsonSerializer.Serialize(payload));
        }

        /// <summary>
        /// Returns the stored user ID, or <c>null</c> if no valid session exists.
        /// Any I/O or parse error is treated as "no session".
        /// </summary>
        public int? LoadSession()
        {
            if (!File.Exists(SessionFilePath)) return null;
            try
            {
                var text = File.ReadAllText(SessionFilePath);
                var payload = JsonSerializer.Deserialize<SessionPayload>(text);
                return payload?.UserId > 0 ? payload.UserId : null;
            }
            catch
            {
                return null;
            }
        }

        public void ClearSession()
        {
            if (File.Exists(SessionFilePath))
                File.Delete(SessionFilePath);
        }

        private sealed class SessionPayload
        {
            public int UserId { get; set; }
        }
    }
}
