// src/SprintBoard.Api/ResultHttpExtensions.cs
using SprintBoard.Domain.Common;

namespace SprintBoard.Api;

public static class ResultHttpExtensions
{
    /// <summary>The single place where domain errors become HTTP status codes.</summary>
    public static IResult ToHttp<T>(this Result<T> result, Func<T, IResult>? onOk = null) =>
        result switch
        {
            Result<T>.Ok ok => onOk is null ? Results.Ok(ok.Value) : onOk(ok.Value),
            Result<T>.Fail fail => fail.Error.Code switch
            {
                ErrorCodes.NotFound => Results.NotFound(fail.Error),
                ErrorCodes.Forbidden => Results.Json(fail.Error, statusCode: StatusCodes.Status403Forbidden),
                ErrorCodes.Validation => Results.BadRequest(fail.Error),
                ErrorCodes.InvalidMove => Results.Conflict(fail.Error),
                _ => Results.Json(fail.Error, statusCode: StatusCodes.Status500InternalServerError)
            },
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
}