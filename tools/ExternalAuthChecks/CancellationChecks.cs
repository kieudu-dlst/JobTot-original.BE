using JobTot.Api;
using JobTot.Application.Persistence;
using JobTot.Domain.Entities;
using JobTot.Infrastructure;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Q = JobTot.Application.Persistence.DataQueries;

public static class CancellationChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var ct = cancellation.Token;
        await using var db = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase("cancellation-" + Guid.NewGuid()).Options);
        var data = new RecruitmentData(db);
        var list = Q.ToListAsync(data.AccountRecords, ct);
        var scalar = Q.AnyAsync(data.AccountRecords, ct);
        // InMemory cannot execute bulk updates: cancellation must stop before translation/execution.
        var update = data.Companies.UpdateAsync(c => c.Set(x => x.Culture, "unused"), ct);
        check(list.IsCanceled && scalar.IsCanceled && update.IsCanceled,
            "already-cancelled queries and updates return cancelled tasks without starting persistence");
        try { await list; check(false, "cancelled task"); }
        catch (OperationCanceledException e) { check(e.CancellationToken == ct, "query cancellation retains its original token"); }

        var http = new DefaultHttpContext { RequestAborted = ct };
        var action = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var filter = new RequestCancellationFilter();
        var resource = new ResourceExecutingContext(action, filters, new List<IValueProviderFactory>());
        var invoked = false;
        await filter.OnResourceExecutionAsync(resource, () =>
        {
            invoked = true;
            return Task.FromResult(new ResourceExecutedContext(action, filters));
        });
        check(!invoked && resource.Result is EmptyResult && http.Response.StatusCode == 499,
            "MVC stops an already-aborted request before model binding and action execution");
        var expected = new ExceptionContext(action, filters) { Exception = new OperationCanceledException(ct) };
        filter.OnException(expected);
        check(expected.ExceptionHandled && expected.Result is EmptyResult && http.Response.Body.Length == 0,
            "MVC handles action cancellation locally without writing a response body");
        var unrelated = new ExceptionContext(action, filters) { Exception = new InvalidOperationException("real error") };
        filter.OnException(unrelated);
        check(!unrelated.ExceptionHandled, "a disconnected client does not hide an unrelated application error");
        check(RequestCancellation.IsClientCancellation(new DataWriteException("wrapped", new DbUpdateException("database", new OperationCanceledException(ct))), ct),
            "wrapped database cancellation is recognized at the HTTP boundary");
        http.RequestAborted = default;
        var serverCancellation = new ExceptionContext(action, filters) { Exception = new OperationCanceledException() };
        filter.OnException(serverCancellation);
        check(!serverCancellation.ExceptionHandled, "server cancellation without client disconnect is not classified as HTTP 499");
        var binding = new ResourceExecutingContext(action, filters, new List<IValueProviderFactory>());
        var failedBinding = new ResourceExecutedContext(action, filters) { Exception = new OperationCanceledException(ct) };
        await filter.OnResourceExecutionAsync(binding, () =>
        {
            http.RequestAborted = ct;
            return Task.FromResult(failedBinding);
        });
        check(failedBinding.ExceptionHandled && failedBinding.Result is EmptyResult,
            "MVC handles cancellation during model binding as well as action execution");

        var middleware = new RequestCancellationMiddleware(_ => Task.FromCanceled(ct));
        var disconnected = new DefaultHttpContext { RequestAborted = ct };
        await middleware.InvokeAsync(disconnected);
        check(disconnected.Response.StatusCode == 499 && disconnected.Response.Body.Length == 0,
            "middleware short-circuits disconnected requests without serialization");
        var racing = new DefaultHttpContext();
        await new RequestCancellationMiddleware(context =>
        {
            context.RequestAborted = ct;
            return Task.FromCanceled(ct);
        }).InvokeAsync(racing);
        check(racing.Response.StatusCode == 499, "middleware handles cancellation occurring after the request has started");
    }
}
