using MentoringApp.Service;

namespace MentoringApp.Api.Helpers;

/// <summary>Translates a service-layer <see cref="Result"/>/<see cref="Result{T}"/> into an HTTP response (200 OK with data, or 400 with the error).</summary>
public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> r) =>
        r.Success ? Results.Ok(r.Data) : Results.BadRequest(new { error = r.ErrorMessage });

    public static IResult ToHttp(this Result r) =>
        r.Success ? Results.Ok() : Results.BadRequest(new { error = r.ErrorMessage });
}
