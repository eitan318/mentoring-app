namespace MentoringApp.ApiClient.Clients;

/// <summary>Typed client for the /api/notifications endpoints (trigger phase-transition emails to users).</summary>
public class NotificationApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task SendPhase1StartedAsync() =>
        PostAsync("/api/notifications/phase1-started");

    public Task SendPhase2StartedAsync() =>
        PostAsync("/api/notifications/phase2-started");
}
