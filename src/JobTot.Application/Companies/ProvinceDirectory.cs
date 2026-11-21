using JobTot.Domain.Enums;

namespace JobTot.Application.Companies;

public sealed record ProvinceItem(Guid Id, string Name);
public sealed class ProvinceDirectory(IRecruitmentData data)
{
    public Task<List<ProvinceItem>> ListAsync(CancellationToken ct) => data.ProvinceRecords.ReadOnly()
        .Where(p => p.Status == EntityStatus.Active).OrderBy(p => p.ProvinceName)
        .Select(p => new ProvinceItem(p.Id, p.ProvinceName)).ToListAsync(ct);
}
