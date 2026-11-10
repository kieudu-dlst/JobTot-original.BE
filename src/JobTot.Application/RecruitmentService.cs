using JobTot.Domain.Entities;

namespace JobTot.Application;

public sealed class RecruitmentService(IRecruitmentRepository repository)
{
    public async Task<PageResult<CompanyDto>> GetCompaniesAsync(
        int page,
        int size,
        CancellationToken ct,
        string? keyword = null
    )
    {
        try
        {
            return await (repository.GetCompaniesAsync(page, size, ct, keyword));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.GetCompaniesAsync"
            );
            throw;
        }
    }

    public async Task<CompanyDto> GetCompanyAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var company =
                await repository.GetCompanyAsync(id, ct)
                ?? throw new NotFoundException("Company not found.");
            return new(company.Id, company.Name, company.Description);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.GetCompanyAsync"
            );
            throw;
        }
    }

    public async Task<CompanyDto> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var company = new Company(request.Name, request.Description);
            repository.AddCompany(company);
            await repository.SaveChangesAsync(ct);
            return new(company.Id, company.Name, company.Description);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.CreateCompanyAsync"
            );
            throw;
        }
    }

    public async Task<PageResult<JobDto>> SearchAsync(JobSearch query, CancellationToken ct)
    {
        try
        {
            return await (repository.SearchJobsAsync(query, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.SearchAsync"
            );
            throw;
        }
    }

    public async Task<JobDirectoryMetadata> GetJobFiltersAsync(CancellationToken ct)
    {
        try
        {
            return await (repository.GetJobFiltersAsync(ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.GetJobFiltersAsync"
            );
            throw;
        }
    }

    public async Task<JobDto> GetAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return Map(await FindAsync(id, ct));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.GetAsync"
            );
            throw;
        }
    }

    public async Task<JobDto> CreateAsync(CreateJobRequest request, CancellationToken ct)
    {
        try
        {
            _ =
                await repository.GetCompanyAsync(request.CompanyId, ct)
                ?? throw new NotFoundException("Company not found.");
            var input = request.Job;
            var provinceId = await repository.ResolveJobProvinceAsync(
                input.ProvinceId,
                input.Location,
                ct
            );
            var job = new JobPost(
                request.CompanyId,
                input.Title,
                input.Description,
                input.Location,
                input.SalaryMin,
                input.SalaryMax,
                input.ExpiresAt
            );
            job.SetCandidateDetails(
                input.Requirements,
                input.HiringProcess,
                input.Vacancies,
                input.GenderRequirement,
                input.AgeRequirement,
                input.RecruiterName,
                input.RecruiterTitle,
                input.RecruiterEmail
            );
            job.SetDirectoryDetails(
                input.ExperienceLevel,
                input.PositionLevel,
                input.EmploymentType,
                input.SalaryPeriod,
                input.WorkSchedule,
                input.SalaryNote,
                input.Benefits
            );
            job.SetProvince(provinceId);
            repository.AddJob(job);
            await repository.SaveChangesAsync(ct);
            return Map(job);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.CreateAsync"
            );
            throw;
        }
    }

    public async Task<JobDto> UpdateAsync(Guid id, SaveJobRequest input, CancellationToken ct)
    {
        try
        {
            var job = await FindAsync(id, ct);
            var provinceId = await repository.ResolveJobProvinceAsync(
                input.ProvinceId,
                input.Location,
                ct
            );
            var retainedProvinceId = input.Location.Trim() == job.Location ? job.ProvinceId : null;
            job.Update(
                input.Title,
                input.Description,
                input.Location,
                input.SalaryMin,
                input.SalaryMax,
                input.ExpiresAt
            );
            job.SetProvince(provinceId ?? retainedProvinceId);
            job.SetCandidateDetails(
                input.Requirements,
                input.HiringProcess,
                input.Vacancies,
                input.GenderRequirement,
                input.AgeRequirement,
                input.RecruiterName,
                input.RecruiterTitle,
                input.RecruiterEmail
            );
            job.SetDirectoryDetails(
                input.ExperienceLevel,
                input.PositionLevel,
                input.EmploymentType,
                input.SalaryPeriod,
                input.WorkSchedule,
                input.SalaryNote,
                input.Benefits
            );
            await repository.SaveChangesAsync(ct);
            return Map(job);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.UpdateAsync"
            );
            throw;
        }
    }

    public async Task CloseAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var job = await FindAsync(id, ct);
            job.Close();
            await repository.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.CloseAsync"
            );
            throw;
        }
    }

    private async Task<JobPost> FindAsync(Guid id, CancellationToken ct)
    {
        try
        {
            return await repository.GetJobAsync(id, ct)
                ?? throw new NotFoundException("Job not found.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "RecruitmentService.FindAsync"
            );
            throw;
        }
    }

    private static JobDto Map(JobPost job)
    {
        try
        {
            return new(
                job.Id,
                job.CompanyId,
                job.Title,
                job.Description,
                job.Location,
                job.SalaryMin,
                job.SalaryMax,
                job.CreatedAt,
                job.ExpiresAt,
                job.IsClosed
            )
            {
                Requirements = job.Requirements,
                HiringProcess = job.HiringProcess,
                Vacancies = job.Vacancies,
                GenderRequirement = job.GenderRequirement,
                AgeRequirement = job.AgeRequirement,
                RecruiterName = job.RecruiterName,
                RecruiterTitle = job.RecruiterTitle,
                RecruiterEmail = job.RecruiterEmail,
                ProvinceId = job.ProvinceId,
                CompanyName = job.Company?.Name,
                CompanyLogoUrl = job.Company?.LogoFile?.Url,
                IsVerified = job.Company?.VerificationStatus == "Verified",
                Industry = job.Company?.Industry?.IndustryName,
                ExperienceLevel = job.ExperienceLevel,
                PositionLevel = job.PositionLevel,
                EmploymentType = job.EmploymentType,
                SalaryPeriod = job.SalaryPeriod,
                WorkSchedule = job.WorkSchedule,
                SalaryNote = job.SalaryNote,
                Benefits = job.Benefits,
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
                "RecruitmentService.Map"
            );
            throw;
        }
    }
}
