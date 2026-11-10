using JobTot.Domain.Entities;

namespace JobTot.Application;

public interface IRecruitmentRepository
{
    Task<Company?> GetCompanyAsync(Guid id, CancellationToken ct);
    Task<PageResult<CompanyDto>> GetCompaniesAsync(
        int page,
        int pageSize,
        CancellationToken ct,
        string? keyword = null
    );
    Task<JobPost?> GetJobAsync(Guid id, CancellationToken ct);
    Task<PageResult<JobDto>> SearchJobsAsync(JobSearch query, CancellationToken ct);
    Task<JobDirectoryMetadata> GetJobFiltersAsync(CancellationToken ct);
    Task<Guid?> ResolveJobProvinceAsync(Guid? provinceId, string location, CancellationToken ct);
    Task<JobSubmissionDto?> GetSubmissionAsync(Guid accountId, Guid jobId, CancellationToken ct);
    Task<JobSubmissionDto> SubmitAsync(
        Guid accountId,
        Guid jobId,
        SubmitJobApplicationRequest input,
        CancellationToken ct
    );
    void AddCompany(Company company);
    void AddJob(JobPost job);
    Task SaveChangesAsync(CancellationToken ct);
}
