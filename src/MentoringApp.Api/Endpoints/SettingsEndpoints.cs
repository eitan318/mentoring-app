using MentoringApp.Service;
using MentoringApp.Model;

namespace MentoringApp.Api.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        // GET /api/settings — returns all settings (accessible to all authenticated users)
        group.MapGet("/", async (SettingsService settings) =>
        {
            var phase1Deadline = await settings.GetPhase1DeadlineAsync();
            var phase2Deadline = await settings.GetPhase2DeadlineAsync();
            return Results.Ok(new
            {
                Phase1Deadline      = phase1Deadline?.ToString("o"),
                Phase2Deadline      = phase2Deadline?.ToString("o"),
                IsPhase1Complete    = await settings.GetIsPhase1CompleteAsync(),
                IsProcessComplete   = await settings.GetIsProcessCompleteAsync(),
                IsSchoolConfigured  = await settings.GetIsSchoolConfiguredAsync(),
                IsSupervisorsAssigned = await settings.GetIsSupervisorsAssignedAsync(),
                IsUsersImported     = await settings.GetIsUsersImportedAsync(),
                MeetingHoursBarrier = await settings.GetMeetingHoursBarrierAsync()
            });
        }).WithOpenApi();

        // PUT /api/settings/phase1-deadline  { "deadline": "2026-05-01T00:00:00" | null }
        group.MapPut("/phase1-deadline", async (DeadlineRequest body, SettingsService settings) =>
        {
            if (body.Deadline == null)
                await settings.ClearPhase1DeadlineAsync();
            else
                await settings.SetPhase1DeadlineAsync(body.Deadline.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // PUT /api/settings/phase2-deadline
        group.MapPut("/phase2-deadline", async (DeadlineRequest body, SettingsService settings) =>
        {
            if (body.Deadline == null)
                await settings.ClearPhase2DeadlineAsync();
            else
                await settings.SetPhase2DeadlineAsync(body.Deadline.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // PUT /api/settings/is-phase1-complete
        group.MapPut("/is-phase1-complete", async (BoolSettingRequest body, SettingsService settings) =>
        {
            await settings.SetIsPhase1CompleteAsync(body.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // PUT /api/settings/is-process-complete
        group.MapPut("/is-process-complete", async (BoolSettingRequest body, SettingsService settings) =>
        {
            await settings.SetIsProcessCompleteAsync(body.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // PUT /api/settings/is-school-configured
        group.MapPut("/is-school-configured", async (BoolSettingRequest body, SettingsService settings) =>
        {
            await settings.SetIsSchoolConfiguredAsync(body.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // PUT /api/settings/is-supervisors-assigned
        group.MapPut("/is-supervisors-assigned", async (BoolSettingRequest body, SettingsService settings) =>
        {
            await settings.SetIsSupervisorsAssignedAsync(body.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // PUT /api/settings/is-users-imported
        group.MapPut("/is-users-imported", async (BoolSettingRequest body, SettingsService settings) =>
        {
            await settings.SetIsUsersImportedAsync(body.Value);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();

        // POST /api/settings/advance-year
        // Graduates the highest-grade students, promotes all others, wipes pairs, resets wizard.
        group.MapPost("/advance-year", async (SystemAdminService adminService) =>
        {
            await adminService.AdvanceYearAsync();
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly").WithOpenApi();
    }
}

