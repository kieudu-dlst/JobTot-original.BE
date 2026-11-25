using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using JobTot.Api.Controllers;
using JobTot.Application.Authentication;
using JobTot.Application.Features;
using JobTot.Application.Persistence;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using JobTot.Infrastructure;
using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Q = JobTot.Application.Persistence.DataQueries;

public static class ArchitectureChecks
{
    public static async Task Run(IServiceProvider services, string origin, Action<bool, string> check)
    {
        await CancellationChecks.Run(check);
        var references = typeof(CandidateAuthService).Assembly.GetReferencedAssemblies();
        check(!references.Any(x => x.Name!.StartsWith("Microsoft.AspNetCore") || x.Name.StartsWith("Microsoft.EntityFrameworkCore") || x.Name is "JobTot.Infrastructure" or "JobTot.Api"), "Application assembly has no HTTP, ORM, or outward project dependency");
        var controllers = typeof(CandidateAuthController).Assembly.GetTypes().Where(t => t.Name.EndsWith("Controller"));
        check(!controllers.SelectMany(t => t.GetConstructors()).SelectMany(c => c.GetParameters()).Any(p => p.ParameterType.Namespace?.StartsWith("JobTot.Infrastructure") == true || p.ParameterType == typeof(IRecruitmentData)), "controllers receive application services instead of database adapters");

        await QuerySpecifications(check);
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecruitmentDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IAccountPasswordHasher>();
        var admin = new Account { Email = "architecture-admin@checks.example", FullName = "Admin", AccountType = "Admin", AccountStatus = "Active" };
        admin.PasswordHash = hasher.Hash(admin, "ArchitectureCheck2026!");
        db.Add(admin);
        await db.SaveChangesAsync();
        using var client = new HttpClient { BaseAddress = new Uri(origin) };
        foreach (var route in new[] { "/api/industries/overview", "/api/career-articles", "/api/employer/auth/provinces" })
            check((await client.GetAsync(route)).StatusCode == HttpStatusCode.OK, "HTTP query use case: " + route);
        var login = await client.PostAsJsonAsync("/api/admin/auth/login", new { emailOrPhone = admin.Email, password = "ArchitectureCheck2026!" });
        check(login.IsSuccessStatusCode, "admin login persists the session through the application port");
        var session = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        check((await client.GetAsync("/api/admin/auth/me")).IsSuccessStatusCode, "admin JWT validates the application session");
        var created = await client.PostAsJsonAsync("/api/admin/industries", new { name = "Architecture " + Guid.NewGuid() });
        var createdIndustry = await created.Content.ReadFromJsonAsync<JsonElement>();
        check(created.StatusCode == HttpStatusCode.Created && created.Headers.Location?.ToString() == "/api/admin/industries/" + createdIndustry.GetProperty("id").GetGuid(), "API maps a created application result to status 201 and its Location header");
        check((await client.PostAsync("/api/admin/auth/logout", null)).StatusCode == HttpStatusCode.NoContent, "admin logout revokes the session");
        check((await client.GetAsync("/api/admin/auth/me")).StatusCode == HttpStatusCode.Unauthorized, "revoked admin session cannot access protected APIs");
        await FeatureChecks.Run(services, origin, check);
    }

    private static async Task QuerySpecifications(Action<bool, string> check)
    {
        var options = new DbContextOptionsBuilder<RecruitmentDbContext>().UseInMemoryDatabase("architecture-" + Guid.NewGuid()).Options;
        await using var db = new RecruitmentDbContext(options);
        var account = new Account { FullName = "Candidate", AccountType = "Candidate", AccountStatus = "Active", Email = "query@checks.example" };
        var profile = new CandidateProfile { Account = account, AccountId = account.Id, Headline = "Engineer" };
        profile.CandidateSkills.Add(new CandidateSkill { Candidate = profile, Skill = new Skill { SkillName = "C#" } });
        profile.CandidateSkills.Add(new CandidateSkill { Candidate = profile, Skill = new Skill { SkillName = "hidden" }, Status = EntityStatus.Inactive });
        db.Add(profile);
        var article = new CareerArticle { Id = Guid.NewGuid(), Title = "Initial", Published = true, Version = Guid.NewGuid(), UpdatedAt = DateTimeOffset.UtcNow };
        db.Add(article);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var data = new RecruitmentData(db);
        var query = data.CandidateProfileRecords.ReadOnlyWithIdentity().SplitQuery()
            .With(x => x.Account).With(x => x.CandidateSkills.Where(s => s.Status == EntityStatus.Active)).ThenWith(s => s.Skill);
        var rows = await Q.ToListAsync(query);
        check(rows.Count == 1 && rows[0].Account.FullName == "Candidate" && rows[0].CandidateSkills.Single().Skill.SkillName == "C#", "query specifications preserve filtered collection includes, reference includes and identity resolution");
        check(!db.ChangeTracker.Entries().Any(), "read-only specifications do not track returned entities");
        var nested = data.CandidateProfileRecords.Where(p => data.AccountRecords.Any(a => a.Id == p.AccountId));
        check(await Q.CountAsync(nested) == 1 && await Q.AnyAsync(nested), "nested specifications and async scalar operators remain server queries");
        check(await Q.MaxAsync(data.CareerArticles, x => (DateTimeOffset?)x.UpdatedAt) == article.UpdatedAt, "async Max preserves nullable scalar semantics");
        var actor = new Actor(account.Id);
        var before = new CareerArticlesService(data, new NoArticleFiles(), actor);
        var outcome = await before.Update(article.Id, new CareerArticlesService.ArticleInput("Changed", "Tips", "summary", "content", true, article.Version), default);
        check(outcome.Kind == JobTot.Application.Execution.ResultKind.Success, "article write use case persists through the unit of work");
        var stale = await before.Update(article.Id, new CareerArticlesService.ArticleInput("Stale", "Tips", "summary", "content", true, Guid.NewGuid()), default);
        check(stale.Kind == JobTot.Application.Execution.ResultKind.Conflict && (await EntityFrameworkQueryableExtensions.SingleAsync(db.CareerArticles)).Title == "Changed", "stale article updates cannot overwrite a newer revision");
        await using var second = new RecruitmentDbContext(options);
        var other = new RecruitmentData(second);
        var firstRow = await Q.SingleAsync(data.CareerArticles);
        var secondRow = await Q.SingleAsync(other.CareerArticles);
        firstRow.Version = Guid.NewGuid();
        await data.SaveChangesAsync();
        secondRow.Version = Guid.NewGuid();
        try { await other.SaveChangesAsync(); check(false, "concurrency exception translation"); }
        catch (DataConcurrencyException) { check(true, "provider concurrency failures become application exceptions"); }
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try { await Q.ToListAsync(query, cancelled.Token); check(false, "query cancellation"); }
        catch (OperationCanceledException) { check(true, "cancelled specifications stop before persistence execution"); }

        await using var retryDb = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseInMemoryDatabase("retry-" + Guid.NewGuid()).ReplaceService<IExecutionStrategyFactory, RetryFactory>().Options);
        var attempts = 0;
        var retried = await new RecruitmentData(retryDb).ExecuteAsync(() =>
        {
            if (++attempts == 1)
                throw new DataWriteException("temporary", new DbUpdateException("retry", new TimeoutException()));
            return Task.FromResult(42);
        });
        check(retried == 42 && attempts == 2, "application transaction retries retain the provider's transient-error classification");

        await using var sql = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseSqlServer("Server=unused;Database=unused;User Id=unused;Password=unused;TrustServerCertificate=true")
            .AddInterceptors(new PreventConnection()).Options);
        var relational = new RecruitmentData(sql);
        check(!sql.Database.HasPendingModelChanges(), "moving domain records leaves the relational schema unchanged");
        var activeAccounts = relational.AccountRecords.ReadOnly().Where(a => a.Status == EntityStatus.Active);
        var sqlQuery = relational.CandidateProfileRecords.ReadOnlyWithIdentity().Where(p => activeAccounts.Any(a => a.Id == p.AccountId))
            .With(x => x.Account).With(x => x.CandidateSkills.Where(s => s.Status == EntityStatus.Active)).ThenWith(s => s.Skill);
        var readMethod = sqlQuery.Provider.GetType().GetMethod("Read")!.MakeGenericMethod(typeof(CandidateProfile));
        var translated = (IQueryable<CandidateProfile>)readMethod.Invoke(sqlQuery.Provider, [sqlQuery.Expression])!;
        var command = translated.ToQueryString();
        check(command.Contains("JOIN") && command.Contains("Skill"), "query specifications translate to SQL Server joins without a database connection");
        try
        {
            await relational.Companies.Where(c => c.Id == Guid.Empty && c.Culture == "old")
                .UpdateAsync(c => c.Set(x => x.Name, "Updated").Set(x => x.Culture, "new").Set(x => x.IndustryId, (Guid?)null), default);
            check(false, "conditional update SQL probe");
        }
        catch (SqlProbeException) { check(true, "conditional update specifications translate before opening a SQL connection"); }
    }

    private sealed class SqlProbeException : Exception;
    private sealed class RetryFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public IExecutionStrategy Create() => new RetryStrategy(dependencies);
    }
    private sealed class RetryStrategy(ExecutionStrategyDependencies dependencies) : ExecutionStrategy(dependencies, 2, TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TimeoutException;
    }
    private sealed class PreventConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken ct = default) => throw new SqlProbeException();
    }
    private sealed class Actor(Guid accountId) : JobTot.Application.Execution.ICurrentActor
    {
        public Guid AccountId => accountId;
        public Guid SessionId => Guid.Empty;
        public bool IsEmployer => false;
        public CancellationToken Cancellation => default;
    }
    private sealed class NoArticleFiles : JobTot.Application.Storage.IArticleImageStorage
    {
        public Task<string> SaveAsync(byte[] content, bool png, CancellationToken ct) => throw new NotSupportedException();
        public void Delete(string url) => throw new NotSupportedException();
    }
}
