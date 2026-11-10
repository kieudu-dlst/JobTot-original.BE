using JobTot.Application;
using JobTot.Application.Authentication;
using JobTot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobTot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContext<RecruitmentDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
        );
        services.AddScoped<IRecruitmentRepository, RecruitmentRepository>();
        services.AddScoped<JobTot.Application.Cvs.ICvRepository, CvRepository>();
        services.AddScoped<
            JobTot.Application.Profiles.ICandidateJobsRepository,
            CandidateJobsRepository
        >();
        services.AddScoped<
            JobTot.Application.Companies.ICompanyDirectoryRepository,
            CompanyDirectoryRepository
        >();
        services.AddScoped<ICandidateAuthRepository, CandidateAuthRepository>();
        services.AddScoped<
            JobTot.Application.Profiles.ICandidateProfileRepository,
            CandidateProfileRepository
        >();
        services.AddScoped<CandidateAuthService>();
        services.AddScoped<IEmployerAuthRepository, EmployerAuthRepository>();
        services.AddScoped<EmployerAuthService>();
        services.AddSingleton<IAccountPasswordHasher, AccountPasswordHasher>();
        return services;
    }
}
