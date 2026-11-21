namespace JobTot.Application.Companies;

public sealed class CompanyDirectoryService(ICompanyDirectoryRepository directory)
{
    public async Task<IReadOnlyList<CompanyListingDto>> FeaturedAsync(CancellationToken ct)
    {
        var result = await directory.SearchAsync(new CompanySearch
        {
            VerifiedOnly = true,
            HiringOnly = true,
            Sort = "jobs",
            PageSize = 4,
        }, ct);
        return result.Items;
    }
}
