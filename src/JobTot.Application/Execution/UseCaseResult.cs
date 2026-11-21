namespace JobTot.Application.Execution;

public interface ICurrentActor
{
    Guid AccountId
    {
        get;
    }
    Guid SessionId
    {
        get;
    }
    bool IsEmployer
    {
        get;
    }
    CancellationToken Cancellation
    {
        get;
    }
}

public enum ResultKind
{
    Success,
    Created,
    Completed,
    NotFound,
    Invalid,
    Unauthenticated,
    Forbidden,
    Conflict,
    Cancelled,
    ExternalFailure,
    ExternalTimeout,
    Unprocessable,
}

public sealed record UseCaseResult(ResultKind Kind, object? Value = null, Guid? ResourceId = null)
{
    public static UseCaseResult Success(object? value = null) => new(ResultKind.Success, value);
    public static UseCaseResult Created(Guid id, object? value) => new(ResultKind.Created, value, id);
    public static UseCaseResult Completed() => new(ResultKind.Completed);
    public static UseCaseResult NotFound(object? value = null) => new(ResultKind.NotFound, value);
    public static UseCaseResult Invalid(object? value = null) => new(ResultKind.Invalid, value);
    public static UseCaseResult Unauthenticated(object? value = null) => new(ResultKind.Unauthenticated, value);
    public static UseCaseResult Forbidden() => new(ResultKind.Forbidden);
    public static UseCaseResult Conflict(object? value = null) => new(ResultKind.Conflict, value);
    public static UseCaseResult Cancelled() => new(ResultKind.Cancelled);
    public static UseCaseResult ExternalFailure(object? value = null) => new(ResultKind.ExternalFailure, value);
    public static UseCaseResult ExternalTimeout(object? value = null) => new(ResultKind.ExternalTimeout, value);
    public static UseCaseResult Unprocessable(object? value = null) => new(ResultKind.Unprocessable, value);
    public static UseCaseResult Download(byte[] content, string contentType, string name) =>
        Success(new FilePayload(content, contentType, name));
}

public sealed record FilePayload(byte[] Content, string ContentType, string Name);
public sealed class FailureDetails
{
    public string? Detail
    {
        get; init;
    }
}

public abstract class ApplicationService(ICurrentActor actor)
{
    protected ICurrentActor Actor => actor;
}
