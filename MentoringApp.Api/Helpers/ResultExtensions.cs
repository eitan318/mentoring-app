using MentoringApp.Service;

namespace MentoringApp.Api.Helpers;

public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> r) =>
        r.Success ? Results.Ok(r.Data) : Results.BadRequest(new { error = r.ErrorMessage });

    public static IResult ToHttp(this Result r) =>
        r.Success ? Results.Ok() : Results.BadRequest(new { error = r.ErrorMessage });
}
