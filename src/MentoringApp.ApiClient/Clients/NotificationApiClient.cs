namespace MentoringApp.ApiClient.Clients;

public class NotificationApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task SendPhase1StartedAsync() =>
        PostAsync("/api/notifications/phase1-started");

    public Task SendPhase2StartedAsync() =>
        PostAsync("/api/notifications/phase2-started");
}
