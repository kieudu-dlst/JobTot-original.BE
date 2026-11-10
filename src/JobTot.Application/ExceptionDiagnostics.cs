namespace JobTot.Application;

/// <summary>Records operation boundaries without replacing an exception or its stack trace.</summary>
public static class ExceptionDiagnostics
{
    public const string OperationsKey = "JobTot.Operations";

    public static void Record(Exception exception, string operation)
    {
        // Cancellation is control flow, including cancellation unrelated to a client disconnect.
        if (exception is OperationCanceledException)
            return;
        if (exception.Data[OperationsKey] is not List<string> operations)
        {
            operations = [];
            exception.Data[OperationsKey] = operations;
        }
        operations.Add(operation);
    }
}
