namespace MentoringApp.Service;

public class AppSettings
{
    public bool RecreateDbOnStartup { get; set; }
    public bool SkipVerificationCode { get; set; }
    public string AdminEmail { get; set; } = "admin@school.edu";
}
