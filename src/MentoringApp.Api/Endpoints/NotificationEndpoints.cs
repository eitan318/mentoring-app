using MentoringApp.Service;

namespace MentoringApp.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization("AdminOnly");

        // POST /api/notifications/phase1-started
        group.MapPost("/phase1-started", async (NotificationService notifications) =>
        {
            bool ok = await notifications.SendPhase1StartedAsync();
            return ok ? Results.Ok() : Results.Ok(new { warning = "Some emails may not have been sent." });
        }).WithOpenApi();

        // POST /api/notifications/phase2-started
        group.MapPost("/phase2-started", async (NotificationService notifications) =>
        {
            bool ok = await notifications.SendPhase2StartedAsync();
            return ok ? Results.Ok() : Results.Ok(new { warning = "Some emails may not have been sent." });
        }).WithOpenApi();
    }
}
