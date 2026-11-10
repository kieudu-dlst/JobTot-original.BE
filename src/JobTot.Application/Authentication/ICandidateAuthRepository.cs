using JobTot.Domain.Entities;

namespace JobTot.Application.Authentication;

public interface ICandidateAuthRepository
{
    Task<Account?> FindAsync(CandidateIdentifier identifier, CancellationToken ct);
    Task<Account?> GetAsync(Guid id, CancellationToken ct);
    Task<string?> GetAvatarUrlAsync(Guid id, CancellationToken ct);
    Task AddAsync(Account account, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<bool> ResetPasswordAsync(
        Guid id,
        string expectedHash,
        string newHash,
        CancellationToken ct
    );
}

public interface IAccountPasswordHasher
{
    string Hash(Account account, string password);
    bool Verify(Account account, string password, out bool needsRehash);
}
