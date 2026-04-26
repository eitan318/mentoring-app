using System.IO;
using System.Text.Json;

namespace MentoringApp.ViewModel.Service;

public class SessionService
{
    private static readonly string SessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MentoringApp", "session.json");

    public void SaveSession(int userId, string token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SessionFilePath)!);
        File.WriteAllText(SessionFilePath, JsonSerializer.Serialize(new SessionPayload { UserId = userId, Token = token }));
    }

    public SessionPayload? LoadSession()
    {
        if (!File.Exists(SessionFilePath)) return null;
        try
        {
            var payload = JsonSerializer.Deserialize<SessionPayload>(File.ReadAllText(SessionFilePath));
            return payload?.UserId > 0 && !string.IsNullOrEmpty(payload.Token) ? payload : null;
        }
        catch { return null; }
    }

    public void ClearSession()
    {
        if (File.Exists(SessionFilePath)) File.Delete(SessionFilePath);
    }

    public sealed class SessionPayload
    {
        public int UserId { get; set; }
        public string Token { get; set; } = "";
    }
}
