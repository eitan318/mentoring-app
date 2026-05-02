using MentoringApp.Model;

namespace MentoringApp.ApiClient.Clients;

public class SettingsApiClient(HttpClient http) : ApiClientBase(http)
{
    public Task<SettingsResponse> GetAllAsync() =>
        GetAsync<SettingsResponse>("/api/settings");

    public Task SetPhase1DeadlineAsync(DateTime? deadline) =>
        PutAsync("/api/settings/phase1-deadline", new DeadlineRequest(deadline));

    public Task SetPhase2DeadlineAsync(DateTime? deadline) =>
        PutAsync("/api/settings/phase2-deadline", new DeadlineRequest(deadline));

    public Task SetIsPhase1CompleteAsync(bool value) =>
        PutAsync("/api/settings/is-phase1-complete", new BoolSettingRequest(value));

    public Task SetIsProcessCompleteAsync(bool value) =>
        PutAsync("/api/settings/is-process-complete", new BoolSettingRequest(value));

    public Task SetIsSchoolConfiguredAsync(bool value) =>
        PutAsync("/api/settings/is-school-configured", new BoolSettingRequest(value));

    public Task SetIsUsersImportedAsync(bool value) =>
        PutAsync("/api/settings/is-users-imported", new BoolSettingRequest(value));
}
