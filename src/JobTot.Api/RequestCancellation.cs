using Microsoft.Data.SqlClient;

namespace JobTot.Api;

public static class RequestCancellation
{
    public static bool IsClientCancellation(
        Exception exception,
        CancellationToken requestAborted
    ) =>
        requestAborted.IsCancellationRequested
        && (
            exception is OperationCanceledException
            || exception is SqlException sql
                && sql.Errors.Cast<SqlError>()
                    .Any(error =>
                        error.Message.Contains(
                            "Operation cancelled by user",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
        );
}
