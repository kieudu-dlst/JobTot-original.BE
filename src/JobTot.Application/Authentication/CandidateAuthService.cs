using System.ComponentModel.DataAnnotations;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;

namespace JobTot.Application.Authentication;

public sealed class CandidateAuthService(
    ICandidateAuthRepository repository,
    IAccountPasswordHasher passwords
)
{
    public const string CandidateRole = "Candidate";
    public const string ActiveStatus = "Active";

    public async Task<string?> GetPasswordStampAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var account = await repository.GetAsync(id, ct);
            return account?.PasswordHash is { } hash
                ? Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(hash)
                    )
                )
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.GetPasswordStampAsync"
            );
            throw;
        }
    }

    public async Task<CandidateAccountDto> RegisterAsync(
        CandidateRegisterRequest request,
        CancellationToken ct
    )
    {
        try
        {
            Validate(request);
            var identifier = CandidateIdentifier.Parse(request.EmailOrPhone);
            if (await repository.FindAsync(identifier, ct) is not null)
                throw new DuplicateAccountException();
            var account = new Account
            {
                FullName = request.FullName.Trim(),
                Email = identifier.Email,
                EmailVerifiedAt = identifier.Email != null ? DateTimeOffset.UtcNow : null,
                Phone = identifier.Phone,
                AccountType = CandidateRole,
                AccountStatus = ActiveStatus,
            };
            account.PasswordHash = passwords.Hash(account, request.Password);
            account.CandidateProfile = new CandidateProfile
            {
                AccountId = account.Id,
                Account = account,
            };
            await repository.AddAsync(account, ct);
            return ToDto(account);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.RegisterAsync"
            );
            throw;
        }
    }

    public async Task<CandidateAccountDto?> LoginAsync(
        CandidateLoginRequest request,
        CancellationToken ct
    )
    {
        try
        {
            Validate(request);
            if (!CandidateIdentifier.TryParse(request.EmailOrPhone, out var identifier))
                return null;
            var account = await repository.FindAsync(identifier!, ct);
            // The password implementation also performs a hash verification for unknown accounts.
            var validPassword = passwords.Verify(
                account ?? new Account(),
                request.Password,
                out var rehash
            );
            if (!validPassword || account is null || !IsActiveCandidate(account))
                return null;
            if (rehash)
            {
                account.PasswordHash = passwords.Hash(account, request.Password);
                await repository.SaveChangesAsync(ct);
            }

            return ToDto(account);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.LoginAsync"
            );
            throw;
        }
    }

    public async Task<CandidateAccountDto?> GetActiveAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var account = await repository.GetAsync(id, ct);
            return account is not null && IsActiveCandidate(account)
                ? ToDto(account) with
                {
                    AvatarUrl = await repository.GetAvatarUrlAsync(id, ct),
                }
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.GetActiveAsync"
            );
            throw;
        }
    }

    public static bool IsActiveCandidate(Account account)
    {
        try
        {
            return account.Status == EntityStatus.Active
                && account.AccountType == CandidateRole
                && account.AccountStatus == ActiveStatus
                && account.CandidateProfile?.Status == EntityStatus.Active;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.IsActiveCandidate"
            );
            throw;
        }
    }

    private static CandidateAccountDto ToDto(Account account)
    {
        try
        {
            return new(
                account.Id,
                account.CandidateProfile!.Id,
                account.FullName,
                account.Email,
                account.Phone,
                CandidateRole
            )
            {
                AvatarUrl = account.CandidateProfile.DetailsJson is { } details
                    ? System
                        .Text.Json.JsonSerializer.Deserialize<JobTot.Application.Profiles.CandidateProfileDocument>(
                            details
                        )
                        ?.AvatarUrl
                    : null,
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.ToDto"
            );
            throw;
        }
    }

    private static void Validate(object request)
    {
        try
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
                throw new ArgumentException(string.Join(" ", errors.Select(x => x.ErrorMessage)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "CandidateAuthService.Validate"
            );
            throw;
        }
    }
}
