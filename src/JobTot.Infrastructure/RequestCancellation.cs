using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using JobTot.Application.Persistence;
using System.Net.Sockets;

namespace JobTot.Infrastructure;

public static class RequestCancellation
{
    public static bool IsClientCancellation(
        Exception exception,
        CancellationToken requestAborted
    ) =>
        requestAborted.IsCancellationRequested
        && (
            exception is OperationCanceledException
            || exception is HttpRequestException
            || exception is IOException { InnerException: SocketException }
            || (exception is DbUpdateException or DataWriteException)
                && exception.InnerException is { } inner
                && IsClientCancellation(inner, requestAborted)
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
