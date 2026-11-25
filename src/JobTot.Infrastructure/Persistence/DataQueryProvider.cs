using System.Collections;
using System.Linq.Expressions;
using JobTot.Application.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace JobTot.Infrastructure.Persistence;

internal sealed class DataQueryProvider(RecruitmentDbContext db) : IDataQueryProvider
{
    private IQueryProvider Provider => ((IQueryable<Domain.Entities.Account>)db.AccountRecords).Provider;
    private static Expression Translate(Expression expression) => new QueryTranslator().Visit(expression)!;
    public IQueryable<T> Read<T>(Expression expression) => Provider.CreateQuery<T>(Translate(expression));
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new DataQuery<TElement>(this, expression);
    public IQueryable CreateQuery(Expression expression) => (IQueryable)Activator.CreateInstance(typeof(DataQuery<>).MakeGenericType(expression.Type.GetGenericArguments()[0]), this, expression)!;
    public object? Execute(Expression expression) => Provider.Execute(Translate(expression));
    public TResult Execute<TResult>(Expression expression) => Provider.Execute<TResult>(Translate(expression));

    private static Task<T> Run<T>(Func<Task<T>> action, CancellationToken ct) =>
        ct.IsCancellationRequested ? Task.FromCanceled<T>(ct) : RunCore(action, ct);

    private static Task<T> RunCore<T>(Func<Task<T>> action, CancellationToken ct)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Fail(Exception error)
        {
            if (ct.IsCancellationRequested && RequestCancellation.IsClientCancellation(error, ct))
                completion.TrySetCanceled(ct);
            else if (error is SqlException { Number: -2 })
                completion.TrySetException(new DataReadTimeoutException(error));
            else
                completion.TrySetException(error);
        }
        void Complete(Task<T> task)
        {
            if (task.IsCanceled)
                completion.TrySetCanceled(ct.IsCancellationRequested ? ct : new CancellationToken(true));
            else if (task.IsFaulted)
            {
                var errors = task.Exception!.InnerExceptions;
                if (errors.Count == 1) Fail(errors[0]);
                else completion.TrySetException(errors);
            }
            else
                completion.TrySetResult(task.Result);
        }
        try
        {
            var task = action();
            if (task.IsCompleted) Complete(task);
            else _ = task.ContinueWith(Complete, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        catch (Exception error)
        {
            Fail(error);
        }
        // Cancellation travels as a cancelled task. Do not throw a new user exception
        // for an expected browser disconnect; the HTTP boundary already handles it.
        return completion.Task;
    }
    public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken ct) =>
        Run(() => ((IAsyncQueryProvider)Provider).ExecuteAsync<Task<TResult>>(Translate(expression), ct), ct);
    public Task<List<T>> ListAsync<T>(Expression expression, CancellationToken ct) => Run(() => EntityFrameworkQueryableExtensions.ToListAsync(Read<T>(expression), ct), ct);
    public Task<int> UpdateAsync<T>(Expression query, Expression<Func<Changes<T>, Changes<T>>> changes, CancellationToken ct) where T : class
        => Run(() =>
    {
        var parameter = Expression.Parameter(typeof(SetPropertyCalls<T>), changes.Parameters[0].Name);
        var body = new ChangeTranslator<T>(changes.Parameters[0], parameter).Visit(changes.Body)!;
        var setters = Expression.Lambda<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>>(body, parameter);
        return Read<T>(query).ExecuteUpdateAsync(setters, ct);
    }, ct);
    private sealed class DataQuery<T>(DataQueryProvider provider, Expression expression) : IOrderedQueryable<T>
    {
        public Type ElementType => typeof(T);
        public Expression Expression => expression;
        public IQueryProvider Provider => provider;
        public IEnumerator<T> GetEnumerator() => provider.Read<T>(expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class QueryTranslator : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            // Specifications may capture another specification (for example an anti-join).
            // Expand it before handing the expression to EF, so nested query shapes are translated too.
            if (typeof(IQueryable).IsAssignableFrom(node.Type) && IsCaptured(node.Expression))
            {
                var value = Expression.Lambda<Func<IQueryable>>(Expression.Convert(node, typeof(IQueryable))).Compile().Invoke();
                if (value.Provider is IDataQueryProvider)
                    return Visit(value.Expression)!;
            }
            return base.VisitMember(node);
        }

        private static bool IsCaptured(Expression? expression) => expression switch
        {
            null or ConstantExpression => true,
            MemberExpression member => IsCaptured(member.Expression),
            _ => false,
        };

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(DataQueries))
                return base.VisitMethodCall(node);
            var name = node.Method.Name switch
            {
                "ReadOnly" => "AsNoTracking",
                "ReadOnlyWithIdentity" => "AsNoTrackingWithIdentityResolution",
                "SplitQuery" => "AsSplitQuery",
                "With" => "Include",
                "ThenWith" => "ThenInclude",
                _ => throw new InvalidOperationException("Unsupported query shape.")
            };
            var owner = name == "AsSplitQuery" ? typeof(RelationalQueryableExtensions) : typeof(EntityFrameworkQueryableExtensions);
            var candidates = owner.GetMethods().Where(m => m.Name == name && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == node.Method.GetGenericArguments().Length && m.GetParameters().Length == node.Arguments.Count);
            if (name == "ThenInclude")
            {
                var collection = node.Method.GetParameters()[0].ParameterType.GetGenericArguments()[1].IsGenericType;
                candidates = candidates.Where(m => m.GetParameters()[0].ParameterType.GetGenericArguments()[1].IsGenericType == collection);
            }
            var method = candidates.Single().MakeGenericMethod(node.Method.GetGenericArguments());
            return Expression.Call(method, node.Arguments.Select(a => Visit(a)!));
        }
    }
    private sealed class ChangeTranslator<T>(ParameterExpression original, ParameterExpression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == original ? replacement : node;
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(Changes<T>))
                return base.VisitMethodCall(node);
            var method = typeof(SetPropertyCalls<T>).GetMethods().Single(m => m.Name == "SetProperty" && !m.GetParameters()[1].ParameterType.IsGenericType).MakeGenericMethod(node.Method.GetGenericArguments());
            return Expression.Call(Visit(node.Object)!, method, node.Arguments.Select(a => Visit(a)!));
        }
    }
}
