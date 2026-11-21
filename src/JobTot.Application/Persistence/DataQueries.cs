using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace JobTot.Application.Persistence;

// The query is a specification. The provider and asynchronous execution live in Infrastructure.
public interface IDataQueryProvider : IQueryProvider
{
    Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken ct);
    Task<List<T>> ListAsync<T>(Expression expression, CancellationToken ct);
    Task<int> UpdateAsync<T>(Expression query, Expression<Func<Changes<T>, Changes<T>>> changes, CancellationToken ct) where T : class;
}

public interface IRelatedQuery<out T, out TProperty> : IQueryable<T>;

public sealed class Changes<T>
{
    private Changes()
    {
    }
    public Changes<T> Set<TProperty>(Func<T, TProperty> property, TProperty value) =>
        throw new InvalidOperationException("Changes are interpreted by the persistence provider.");
}

public static class DataQueries
{
    private sealed class RelatedQuery<T, TProperty>(IQueryable<T> query) : IRelatedQuery<T, TProperty>
    {
        public Type ElementType => typeof(T);
        public Expression Expression => query.Expression;
        public IQueryProvider Provider => query.Provider;
        public IEnumerator<T> GetEnumerator() => query.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static IDataQueryProvider Provider<T>(IQueryable<T> source) =>
        source.Provider as IDataQueryProvider ?? throw new InvalidOperationException("A data query provider is required.");
    private static IQueryable<T> Shape<T>(IQueryable<T> source, MethodInfo method, params Expression[] args) =>
        source.Provider.CreateQuery<T>(Expression.Call(null, method, new[] { source.Expression }.Concat(args)));
    private static MethodInfo Current(MethodBase method, params Type[] types) => ((MethodInfo)method).GetGenericMethodDefinition().MakeGenericMethod(types);

    public static IQueryable<T> ReadOnly<T>(this IQueryable<T> source) where T : class =>
        Shape(source, Current(MethodBase.GetCurrentMethod()!, typeof(T)));
    public static IQueryable<T> ReadOnlyWithIdentity<T>(this IQueryable<T> source) where T : class =>
        Shape(source, Current(MethodBase.GetCurrentMethod()!, typeof(T)));
    public static IQueryable<T> SplitQuery<T>(this IQueryable<T> source) where T : class =>
        Shape(source, Current(MethodBase.GetCurrentMethod()!, typeof(T)));
    public static IRelatedQuery<T, TProperty> With<T, TProperty>(this IQueryable<T> source, Expression<Func<T, TProperty>> path) where T : class =>
        new RelatedQuery<T, TProperty>(Shape(source, Current(MethodBase.GetCurrentMethod()!, typeof(T), typeof(TProperty)), Expression.Quote(path)));
    public static IRelatedQuery<T, TProperty> ThenWith<T, TPrevious, TProperty>(this IRelatedQuery<T, TPrevious> source, Expression<Func<TPrevious, TProperty>> path) where T : class =>
        new RelatedQuery<T, TProperty>(Shape(source, Current(MethodBase.GetCurrentMethod()!, typeof(T), typeof(TPrevious), typeof(TProperty)), Expression.Quote(path)));
    public static IRelatedQuery<T, TProperty> ThenWith<T, TPrevious, TProperty>(this IRelatedQuery<T, IEnumerable<TPrevious>> source, Expression<Func<TPrevious, TProperty>> path) where T : class =>
        new RelatedQuery<T, TProperty>(Shape(source, Current(MethodBase.GetCurrentMethod()!, typeof(T), typeof(TPrevious), typeof(TProperty)), Expression.Quote(path)));

    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => Provider(source).ListAsync<T>(source.Expression, ct);
    private static Task<R> Scalar<T, R>(IQueryable<T> source, string method, CancellationToken ct) =>
        Provider(source).ExecuteAsync<R>(Expression.Call(typeof(Queryable), method, new[] { typeof(T) }, source.Expression), ct);
    public static Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => Scalar<T, bool>(source, nameof(Queryable.Any), ct);
    public static Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => source.Where(predicate).AnyAsync(ct);
    public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => Scalar<T, int>(source, nameof(Queryable.Count), ct);
    public static Task<int> CountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => source.Where(predicate).CountAsync(ct);
    public static Task<T> SingleAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => Scalar<T, T>(source, nameof(Queryable.Single), ct);
    public static Task<T> SingleAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => source.Where(predicate).SingleAsync(ct);
    public static Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => Scalar<T, T?>(source, nameof(Queryable.SingleOrDefault), ct);
    public static Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => source.Where(predicate).SingleOrDefaultAsync(ct);
    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => Scalar<T, T?>(source, nameof(Queryable.FirstOrDefault), ct);
    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => source.Where(predicate).FirstOrDefaultAsync(ct);
    public static Task<R?> MaxAsync<T, R>(this IQueryable<T> source, Expression<Func<T, R?>> selector, CancellationToken ct = default) =>
        Scalar<R?, R?>(source.Select(selector), nameof(Queryable.Max), ct);
    public static Task<int> UpdateAsync<T>(this IQueryable<T> source, Expression<Func<Changes<T>, Changes<T>>> changes, CancellationToken ct) where T : class =>
        Provider(source).UpdateAsync(source.Expression, changes, ct);
}
