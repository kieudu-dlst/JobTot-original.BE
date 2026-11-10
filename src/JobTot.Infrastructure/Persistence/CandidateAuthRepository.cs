using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JobTot.Infrastructure.Persistence;

public sealed class CandidateAuthRepository(RecruitmentDbContext db) : ICandidateAuthRepository
{
    public async Task<Account?> FindAsync(CandidateIdentifier identifier, CancellationToken ct)
    {
        try
        {
            return await (
                identifier.Email is not null
                    ? db
                        .AccountRecords.Include(x => x.CandidateProfile)
                        .SingleOrDefaultAsync(x => x.Email == identifier.Email, ct)
                    : db
                        .AccountRecords.Include(x => x.CandidateProfile)
                        .SingleOrDefaultAsync(x => x.Phone == identifier.Phone, ct)
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
                "CandidateAuthRepository.FindAsync"
            );
            throw;
        }
    }

    public async Task<Account?> GetAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await (
                db
                    .AccountRecords.AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new Account
                    {
                        Id = x.Id,
                        FullName = x.FullName,
                        Email = x.Email,
                        Phone = x.Phone,
                        PasswordHash = x.PasswordHash,
                        AccountType = x.AccountType,
                        AccountStatus = x.AccountStatus,
                        Status = x.Status,
                        CandidateProfile =
                            x.CandidateProfile == null
                                ? null
                                : new CandidateProfile
                                {
                                    Id = x.CandidateProfile.Id,
                                    AccountId = x.Id,
                                    Status = x.CandidateProfile.Status,
                                },
                    })
                    .SingleOrDefaultAsync(ct)
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
                "CandidateAuthRepository.GetAsync"
            );
            throw;
        }
    }

    public async Task<string?> GetAvatarUrlAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var details = await db
                .CandidateProfileRecords.AsNoTracking()
                .Where(p => p.AccountId == id)
                .Select(p => p.DetailsJson)
                .SingleOrDefaultAsync(ct);
            return details is null
                ? null
                : System
                    .Text.Json.JsonSerializer.Deserialize<JobTot.Application.Profiles.CandidateProfileDocument>(
                        details
                    )
                    ?.AvatarUrl;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthRepository.GetAvatarUrlAsync"
            );
            throw;
        }
    }

    public async Task AddAsync(Account account, CancellationToken ct)
    {
        try
        {
            db.AccountRecords.Add(account);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Database uniqueness also handles concurrent registrations.
                throw new DuplicateAccountException();
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthRepository.AddAsync"
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
                "CandidateAuthRepository.SaveChangesAsync"
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
                    .AccountRecords.Where(x => x.Id == id && x.PasswordHash == expectedHash)
                    .ExecuteUpdateAsync(
                        update => update.SetProperty(x => x.PasswordHash, newHash),
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
                "CandidateAuthRepository.ResetPasswordAsync"
            );
            throw;
        }
    }
}
