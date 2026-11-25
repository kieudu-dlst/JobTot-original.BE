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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
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
        var companyService = new JobTot.Application.Features.EmployerCompanyService(data, new UnusedCompanyImages(), new EmployerActor());
        foreach (var result in new[]
        {
            await companyService.Get(ct),
            await companyService.Industries(ct),
            await companyService.Save(new JobTot.Application.Companies.EmployerCompanyDocument(), ct),
            await companyService.UploadImage("logo", new JobTot.Application.Storage.UploadFile(10, "test.png", "image/png",
                (_, _) => throw new Exception("Cancelled request must not read file")), "unused", ct)
        })
            check(result.Kind == JobTot.Application.Execution.ResultKind.Cancelled,
                "company endpoint handles cancellation as a result instead of propagating an exception");
        var companyController = new JobTot.Api.Controllers.EmployerCompanyController(companyService);
        var cancelledResponse = await companyController.Get(ct);
        check(cancelledResponse.Result is StatusCodeResult { StatusCode: 499 },
            "company controller returns an empty HTTP 499 for cancelled requests");
        check((await companyService.Get(CancellationToken.None)).Kind == JobTot.Application.Execution.ResultKind.NotFound,
            "missing company remains NotFound when the request is not cancelled");
        var headerService = new JobTot.Application.Features.AccountHeaderService(data, new EmployerActor());
        check((await headerService.Get(ct)).Kind == JobTot.Application.Execution.ResultKind.Cancelled,
            "already-aborted header request returns Cancelled before starting queries");
        check((await headerService.MarkRead(new(["interview-test"]), ct)).Kind == JobTot.Application.Execution.ResultKind.Cancelled,
            "already-aborted mark-read request does not change notification data");
        var headerController = new JobTot.Api.Controllers.AccountHeaderController(headerService);
        check(await headerController.Get(ct) is StatusCodeResult { StatusCode: 499 }
            && await headerController.MarkRead(new(["interview-test"]), ct) is StatusCodeResult { StatusCode: 499 },
            "header endpoints map expected cancellation to empty HTTP 499 responses");
        using var headerCancellation = new CancellationTokenSource();
        var interviewInterceptor = new InterviewQueryInterceptor(() => headerCancellation.Cancel());
        await using var headerDb = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase("header-cancellation-" + Guid.NewGuid()).EnableServiceProviderCaching(false)
            .AddInterceptors(interviewInterceptor).Options);
        var interruptedHeader = new JobTot.Application.Features.AccountHeaderService(new RecruitmentData(headerDb), new EmployerActor());
        check((await interruptedHeader.Get(headerCancellation.Token)).Kind == JobTot.Application.Execution.ResultKind.Cancelled
            && interviewInterceptor.Triggered,
            "cancellation at the interview query shown in the screenshot is handled inside header service");
        using var countCancellation = new CancellationTokenSource();
        var countInterceptor = new InterviewQueryInterceptor(() => countCancellation.Cancel(), typeof(ChatMessage));
        await using var countDb = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase("header-count-cancellation-" + Guid.NewGuid()).EnableServiceProviderCaching(false)
            .AddInterceptors(countInterceptor).Options);
        var countHeader = new JobTot.Application.Features.AccountHeaderService(new RecruitmentData(countDb), new EmployerActor());
        check((await countHeader.Get(countCancellation.Token)).Kind == JobTot.Application.Execution.ResultKind.Cancelled
            && countInterceptor.Triggered,
            "unread conversation count cancellation returns Cancelled rather than an application failure");
        var realQueryFailure = new InvalidOperationException("real interview query failure");
        await using var failedHeaderDb = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase("header-failure-" + Guid.NewGuid()).EnableServiceProviderCaching(false)
            .AddInterceptors(new InterviewQueryInterceptor(() => throw realQueryFailure)).Options);
        try
        {
            await new JobTot.Application.Features.AccountHeaderService(new RecruitmentData(failedHeaderDb), new EmployerActor()).Get(default);
            check(false, "expected header query failure");
        }
        catch (InvalidOperationException e) { check(ReferenceEquals(e, realQueryFailure), "real interview query failures are not disguised as cancellation or empty success"); }

        var providerType = typeof(RecruitmentData).Assembly.GetType("JobTot.Infrastructure.Persistence.DataQueryProvider")!;
        var runCore = providerType.GetMethod("RunCore", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(int));
        using var inFlight = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = (Task<int>)runCore.Invoke(null, [new Func<Task<int>>(async () =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, inFlight.Token);
            return 1;
        }), inFlight.Token])!;
        await started.Task;
        inFlight.Cancel();
        try { await pending; check(false, "expected in-flight cancellation"); }
        catch (OperationCanceledException e)
        {
            check(pending.IsCanceled && e.CancellationToken == inFlight.Token,
                "in-flight EF cancellation completes a cancelled task without throwing a wrapper exception");
        }
        var unexpected = new OperationCanceledException("provider cancellation without request abort");
        var unrelatedTask = (Task<int>)runCore.Invoke(null,
            [new Func<Task<int>>(() => Task.FromException<int>(unexpected)), CancellationToken.None])!;
        try { await unrelatedTask; check(false, "expected unrelated provider cancellation"); }
        catch (OperationCanceledException e)
        { check(ReferenceEquals(e, unexpected), "provider cancellation without request abort is not misclassified"); }
        var success = (Task<int>)runCore.Invoke(null,
            [new Func<Task<int>>(() => Task.FromResult(42)), CancellationToken.None])!;
        check(await success == 42, "completed query result is preserved");
        var actualFailure = new InvalidOperationException("actual database error");
        foreach (var synchronous in new[] { false, true })
        {
            Func<Task<int>> actionFailure = synchronous
                ? () => throw actualFailure
                : () => Task.FromException<int>(actualFailure);
            var failed = (Task<int>)runCore.Invoke(null, [actionFailure, ct])!;
            try { await failed; check(false, "expected actual failure"); }
            catch (InvalidOperationException e)
            { check(failed.IsFaulted && ReferenceEquals(e, actualFailure), "real failure remains faulted even with cancelled request token"); }
        }
        var synchronousCancellation = (Task<int>)runCore.Invoke(null,
            [new Func<Task<int>>(() => throw new OperationCanceledException(ct)), ct])!;
        check(synchronousCancellation.IsCanceled, "synchronous cancellation returns a cancelled task without rethrowing a wrapper");

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
        var timeout = new DataReadTimeoutException(new Exception("private SQL details"));
        check(!RequestCancellation.IsClientCancellation(timeout, ct),
            "SQL timeout is not misclassified as client cancellation even after disconnect");
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var timeoutContext = new DefaultHttpContext { RequestServices = services };
        timeoutContext.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandler(Microsoft.Extensions.Logging.Abstractions.NullLogger<ApiExceptionHandler>.Instance,
            new DataFailureClassifier());
        await handler.TryHandleAsync(timeoutContext, timeout, CancellationToken.None);
        timeoutContext.Response.Body.Position = 0;
        var payload = await new StreamReader(timeoutContext.Response.Body).ReadToEndAsync();
        check(timeoutContext.Response.StatusCode == 503 && !payload.Contains("private SQL details"),
            "SQL timeout returns a safe HTTP 503 without exposing provider details");
    }
    private sealed class EmployerActor : JobTot.Application.Execution.ICurrentActor
    {
        public Guid AccountId => Guid.Empty;
        public Guid SessionId => Guid.Empty;
        public bool IsEmployer => true;
        public CancellationToken Cancellation => CancellationToken.None;
    }
    private sealed class InterviewQueryInterceptor(Action onInterview, Type? entityType = null) : ExpressionVisitor, IQueryExpressionInterceptor
    {
        public bool Triggered { get; private set; }
        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
            => Visit(queryExpression);
        protected override Expression VisitExtension(Expression node)
        {
            if (!Triggered && node is EntityQueryRootExpression root && root.EntityType.ClrType == (entityType ?? typeof(Interview)))
            {
                Triggered = true;
                onInterview();
            }
            return base.VisitExtension(node);
        }
    }
    private sealed class UnusedCompanyImages : JobTot.Application.Storage.ICompanyImageStorage
    {
        public string DirectoryPath => "unused";
        public Task<string> SaveAsync(byte[] content, string kind, bool png, CancellationToken ct)
            => throw new Exception("Cancelled request must not write images");
    }
}
