using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using JobTot.Domain.Entities;
using JobTot.Domain.Enums;

namespace JobTot.Application.Authentication;

public sealed class EmployerAuthService(
    IEmployerAuthRepository repository,
    IAccountPasswordHasher passwords
)
{
    public const string EmployerRole = "Employer";
    public const string ActiveStatus = "Active";

    public async Task<EmployerRegistrationResult> RegisterAsync(
        EmployerRegisterRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
                throw new ArgumentException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
            var email = request.Email.Trim().ToLowerInvariant();
            var phone = NormalizePhone(request.WorkPhone);
            if (
                await repository.FindAsync(new(email, null), ct) is not null
                || await repository.FindAsync(new(null, phone), ct) is not null
            )
                return new(EmployerRegistrationStatus.DuplicateAccount);
            if (request.FullName.Trim().Length < 2 || request.CompanyName.Trim().Length < 2)
                throw new ArgumentException(
                    "Vui lòng nhập đầy đủ họ tên và tên pháp lý của doanh nghiệp."
                );
            var company = new Company(request.CompanyName.Trim(), "")
            {
                LegalName = request.CompanyName.Trim(),
                TaxCode = request.TaxCode,
                Email = email,
                Phone = phone,
                HeadOfficeProvinceId = request.HeadOfficeProvinceId,
                VerificationStatus = "Pending",
            };
            var account = new Account
            {
                FullName = request.FullName.Trim(),
                Email = email,
                EmailVerifiedAt = DateTimeOffset.UtcNow,
                Phone = phone,
                AccountType = EmployerRole,
                AccountStatus = ActiveStatus,
            };
            account.PasswordHash = passwords.Hash(account, request.Password);
            account.RecruiterProfiles.Add(
                new RecruiterProfile
                {
                    Account = account,
                    AccountId = account.Id,
                    Company = company,
                    CompanyId = company.Id,
                    WorkPhone = phone,
                    JoinedAt = DateTimeOffset.UtcNow,
                    IsCompanyAdmin = true,
                }
            );
            var status = await repository.AddAsync(account, ct);
            return new(
                status,
                status == EmployerRegistrationStatus.Created ? ToDto(account) : null
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
                "EmployerAuthService.RegisterAsync"
            );
            throw;
        }
    }

    public async Task<EmployerAccountDto?> LoginAsync(
        EmployerLoginRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var account = await repository.FindAsync(
                new(request.Email.Trim().ToLowerInvariant(), null),
                ct
            );
            var valid = passwords.Verify(
                account ?? new Account(),
                request.Password,
                out var rehash
            );
            // Invalid credentials are an expected login outcome, not an exception.
            if (!valid || account is null || !IsActiveEmployer(account))
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
                "EmployerAuthService.LoginAsync"
            );
            throw;
        }
    }

    public async Task<EmployerAccountDto?> GetActiveAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var account = await repository.GetAsync(id, ct);
            return account is not null && IsActiveEmployer(account) ? ToDto(account) : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthService.GetActiveAsync"
            );
            throw;
        }
    }

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
                "EmployerAuthService.GetPasswordStampAsync"
            );
            throw;
        }
    }

    public static bool IsActiveEmployer(Account account)
    {
        try
        {
            return account.Status == EntityStatus.Active
                && account.AccountType == EmployerRole
                && account.AccountStatus == ActiveStatus
                && account.RecruiterProfiles.Any(p =>
                    p.Status == EntityStatus.Active && p.Company.Status == EntityStatus.Active
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
                "EmployerAuthService.IsActiveEmployer"
            );
            throw;
        }
    }

    private static EmployerAccountDto ToDto(Account account)
    {
        try
        {
            var profile = account
                .RecruiterProfiles.Where(p =>
                    p.Status == EntityStatus.Active && p.Company.Status == EntityStatus.Active
                )
                .OrderBy(p => p.JoinedAt)
                .ThenBy(p => p.Id)
                .First();
            return new(
                account.Id,
                profile.Id,
                account.FullName,
                account.Email,
                profile.WorkPhone,
                EmployerRole,
                profile.CompanyId,
                profile.Company.Name,
                profile.Company.TaxCode,
                profile.Company.HeadOfficeProvinceId,
                profile.Company.VerificationStatus
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
                "EmployerAuthService.ToDto"
            );
            throw;
        }
    }

    public static string NormalizePhone(string input)
    {
        try
        {
            var digits = Regex.Replace(input.Trim(), @"[\s().-]", "");
            if (digits.StartsWith("+84"))
                digits = "0" + digits[3..];
            else if (digits.StartsWith("84"))
                digits = "0" + digits[2..];
            if (!Regex.IsMatch(digits, @"^0[1-9][0-9]{8,9}$"))
                throw new ArgumentException("Số điện thoại công việc không hợp lệ.");
            return "+84" + digits[1..];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "EmployerAuthService.NormalizePhone"
            );
            throw;
        }
    }
}
