namespace JobTot.Application.Execution;

public static class CancellationPolicy
{
    public static bool IsCancellation(Exception exception, CancellationToken requestAborted) =>
        requestAborted.IsCancellationRequested && exception is OperationCanceledException;
}
