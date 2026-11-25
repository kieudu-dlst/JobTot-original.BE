using System.Diagnostics;
using JobTot.Application.Execution;
using JobTot.Application.Features;
using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class DatabaseReadChecks
{
    // Explicit opt-in: read-only queries against the local development database.
    public static async Task Run()
    {
        await using var db = new RecruitmentDbContext(new DbContextOptionsBuilder<RecruitmentDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=JobTot;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var accounts = await db.RecruiterProfileRecords.AsNoTracking()
            .Where(p => p.Status == JobTot.Domain.Enums.EntityStatus.Active
                && p.Company.Status == JobTot.Domain.Enums.EntityStatus.Active)
            .OrderByDescending(p => p.Company.RecruitmentCampaigns.Count())
            .Select(p => p.AccountId).Take(3).ToListAsync();
        if (accounts.Count == 0) throw new Exception("No active recruiter found for database read checks");
        foreach (var accountId in accounts)
        {
            var service = new EmployerCampaignsService(new RecruitmentData(db), new Actor(accountId));
            var timer = Stopwatch.StartNew();
            var result = await service.List(null, null);
            if (result.Kind != ResultKind.Success) throw new Exception("Employer dashboard query failed: " + result.Kind);
            Console.WriteLine($"PASS employer dashboard/list SQL queries: {timer.ElapsedMilliseconds} ms");
            db.ChangeTracker.Clear();
        }
    }
    private sealed class Actor(Guid accountId) : ICurrentActor
    {
        public Guid AccountId => accountId;
        public Guid SessionId => Guid.Empty;
        public bool IsEmployer => true;
        public CancellationToken Cancellation => CancellationToken.None;
    }
}
