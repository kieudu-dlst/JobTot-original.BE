using JobTot.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class DataFailureClassifier : IDataFailureClassifier
{
    public bool IsConcurrency(Exception exception) => exception is DataConcurrencyException or DbUpdateConcurrencyException;
}
