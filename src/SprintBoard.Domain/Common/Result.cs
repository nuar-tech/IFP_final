// src/SprintBoard.Domain/Common/Result.cs
namespace SprintBoard.Domain.Common;

public sealed record Error(string Code, string Message);

public static class ErrorCodes
{
    public const string NotFound = "not_found";
    public const string Forbidden = "forbidden";
    public const string Validation = "validation";
    public const string InvalidMove = "invalid_move";
}

public static class Errors
{
    public static readonly Error NotFound = new(ErrorCodes.NotFound, "Resource not found.");
    public static readonly Error Forbidden = new(ErrorCodes.Forbidden, "You do not have permission for this action.");
    public static Error Validation(string message) => new(ErrorCodes.Validation, message);
    public static Error InvalidMove(string message) => new(ErrorCodes.InvalidMove, message);
}

/// <summary>A value that is either Ok or Fail. Callers must handle both cases.</summary>
public abstract record Result<T>
{
    private Result() { }

    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fail(Error Error) : Result<T>;
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => new Result<T>.Ok(value);
    public static Result<T> Fail<T>(Error error) => new Result<T>.Fail(error);
    public static Result<T> Fail<T>(string code, string message) => Fail<T>(new Error(code, message));
}

public static class ResultExtensions
{
    public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> ok, Func<Error, TOut> fail) =>
        result switch
        {
            Result<T>.Ok o => ok(o.Value),
            Result<T>.Fail f => fail(f.Error),
            _ => throw new InvalidOperationException("Unknown result case.")
        };

    public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> map) =>
        result.Match(v => Result.Ok(map(v)), e => Result.Fail<TOut>(e));

    public static Result<TOut> Bind<T, TOut>(this Result<T> result, Func<T, Result<TOut>> bind) =>
        result.Match(bind, e => Result.Fail<TOut>(e));
}