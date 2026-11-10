using System.Data;
using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class EmployerAuthRepository(RecruitmentDbContext db) : IEmployerAuthRepository
{
    private IQueryable<Account> Accounts =>
        db.AccountRecords.Include(a => a.RecruiterProfiles).ThenInclude(p => p.Company);

    public async Task<Account?> FindAsync(CandidateIdentifier identifier, CancellationToken ct)
    {
        try
        {
            return await (
                identifier.Email is not null
                    ? Accounts.SingleOrDefaultAsync(a => a.Email == identifier.Email, ct)
                    : Accounts.SingleOrDefaultAsync(a => a.Phone == identifier.Phone, ct)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthRepository.FindAsync"
            );
            throw;
        }
    }

    public async Task<Account?> GetAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await (Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthRepository.GetAsync"
            );
            throw;
        }
    }

    public async Task<EmployerRegistrationStatus> AddAsync(Account account, CancellationToken ct)
    {
        try
        {
            // Serialize tax-code checks and the account/company insert. Never claim an existing company.
            return await db
                .Database.CreateExecutionStrategy()
                .ExecuteAsync(async () =>
                {
                    await using var transaction = db.Database.IsRelational()
                        ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                        : null;
                    var company = account.RecruiterProfiles.Single().Company;
                    if (
                        !await db.ProvinceRecords.AnyAsync(
                            p =>
                                p.Id == company.HeadOfficeProvinceId
                                && p.Status == EntityStatus.Active,
                            ct
                        )
                    )
                        throw new ArgumentException(
                            "Vui lòng chọn tỉnh/thành hợp lệ cho trụ sở chính."
                        );
                    if (
                        await db.AccountRecords.AnyAsync(
                            a => a.Email == account.Email || a.Phone == account.Phone,
                            ct
                        )
                    )
                        return EmployerRegistrationStatus.DuplicateAccount;
                    if (await db.Companies.AnyAsync(c => c.TaxCode == company.TaxCode, ct))
                        return EmployerRegistrationStatus.CompanyConflict;
                    db.AccountRecords.Add(account);
                    try
                    {
                        await db.SaveChangesAsync(ct);
                    }
                    catch (DbUpdateException ex)
                        when (ex.InnerException is SqlException { Number: 2601 or 2627 })
                    {
                        if (transaction is not null)
                        {
                            await transaction.RollbackAsync(ct);
                            await transaction.DisposeAsync();
                        }

                        db.ChangeTracker.Clear();
                        if (
                            await db.AccountRecords.AnyAsync(
                                a => a.Email == account.Email || a.Phone == account.Phone,
                                ct
                            )
                        )
                            return EmployerRegistrationStatus.DuplicateAccount;
                        throw; // An unrelated unique constraint violation remains a database failure.
                    }

                    if (transaction is not null)
                        await transaction.CommitAsync(ct);
                    return EmployerRegistrationStatus.Created;
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthRepository.AddAsync"
            );
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthRepository.SaveChangesAsync"
            );
            throw;
        }
    }

    public async Task<bool> ResetPasswordAsync(
        Guid id,
        string expectedHash,
        string newHash,
        CancellationToken ct
    )
    {
        try
        {
            return await db
                    .AccountRecords.Where(a =>
                        a.Id == id
                        && a.PasswordHash == expectedHash
                        && a.AccountType == EmployerAuthService.EmployerRole
                    )
                    .ExecuteUpdateAsync(
                        update => update.SetProperty(a => a.PasswordHash, newHash),
                        ct
                    ) == 1;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthRepository.ResetPasswordAsync"
            );
            throw;
        }
    }
}
