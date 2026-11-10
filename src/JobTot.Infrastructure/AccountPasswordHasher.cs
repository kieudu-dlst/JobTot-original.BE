using JobTot.Application.Authentication;
using JobTot.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace JobTot.Infrastructure;

public sealed class AccountPasswordHasher : IAccountPasswordHasher
{
    private readonly PasswordHasher<Account> hasher = new();
    private readonly string dummyHash;

    public AccountPasswordHasher() =>
        dummyHash = hasher.HashPassword(new Account(), Guid.NewGuid().ToString());

    public string Hash(Account account, string password) => hasher.HashPassword(account, password);

    public bool Verify(Account account, string password, out bool needsRehash)
    {
        needsRehash = false;
        PasswordVerificationResult result;
        try
        {
            result = hasher.VerifyHashedPassword(
                account,
                account.PasswordHash ?? dummyHash,
                password
            );
        }
        catch (FormatException)
        {
            return false;
        }
        needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
        return account.PasswordHash is not null && result != PasswordVerificationResult.Failed;
    }
}
