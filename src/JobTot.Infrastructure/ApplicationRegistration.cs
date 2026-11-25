using JobTot.Application.Authentication;
using JobTot.Application.Companies;
using JobTot.Application.Execution;
using JobTot.Application.Features;
using JobTot.Application.Storage;
using JobTot.Application.Persistence;
using JobTot.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace JobTot.Infrastructure;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddScoped<JobTot.Application.RecruitmentService>();
        services.TryAddScoped<CandidateAuthService>();
        services.TryAddScoped<EmployerAuthService>();
        services.TryAddScoped<AccountHeaderService>();
        services.TryAddScoped<AccountMessagesService>();
        services.TryAddScoped<AdminCvTemplatesService>();
        services.TryAddScoped<AdminManagementService>();
        services.TryAddScoped<CareerArticlesService>();
        services.TryAddScoped<CompanyMapService>();
        services.TryAddScoped<DemoDataService>();
        services.TryAddScoped<EmployerCampaignsService>();
        services.TryAddScoped<EmployerCandidatesService>();
        services.TryAddScoped<EmployerCompanyService>();
        services.TryAddScoped<EmployerTalentDiscoveryService>();
        services.TryAddScoped<IndustriesService>();
        services.TryAddScoped<CandidateProfileService>();
        services.TryAddScoped<CandidateAccess>();
        services.TryAddScoped<EmployerAccess>();
        services.TryAddScoped<JobTot.Application.Cvs.CvDecorationService>();
        services.TryAddScoped<AdminProvisioning>();
        services.TryAddScoped<AdminSessions>();
        services.TryAddScoped<ProvinceDirectory>();
        services.TryAddScoped<CompanyDirectoryService>();
        services.TryAddScoped<AvatarMigration>();
        services.TryAddScoped<PasswordRecovery>();
        services.TryAddScoped<EmployerPasswordRecovery>();
        services.TryAddScoped<AccountEmailSecurity>();
        services.TryAddScoped<ExternalAuthService>();
        services.TryAddScoped<CandidateTokens>();
        services.TryAddScoped<EmployerTokens>();
        return services;
    }
    public static IServiceCollection AddInfrastructureAdapters(this IServiceCollection services)
    {
        services.TryAddSingleton<IChatEvents, NullChatEvents>();
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddDataProtection();
        services.TryAddScoped<JobTot.Application.IRecruitmentRepository, RecruitmentRepository>();
        services.TryAddScoped<JobTot.Application.Cvs.ICvRepository, CvRepository>();
        services.TryAddScoped<JobTot.Application.Cvs.ICvDecorationStorage, CvDecorationStorage>();
        services.TryAddScoped<JobTot.Application.Profiles.ICandidateJobsRepository, CandidateJobsRepository>();
        services.TryAddScoped<JobTot.Application.Profiles.ICandidateProfileRepository, CandidateProfileRepository>();
        services.TryAddScoped<JobTot.Application.Companies.ICompanyDirectoryRepository, CompanyDirectoryRepository>();
        services.TryAddScoped<ICandidateAuthRepository, CandidateAuthRepository>();
        services.TryAddScoped<IEmployerAuthRepository, EmployerAuthRepository>();
        services.TryAddSingleton<IAccountPasswordHasher, AccountPasswordHasher>();
        services.TryAddScoped<IRecruitmentData, RecruitmentData>();
        services.TryAddSingleton<IDataFailureClassifier, DataFailureClassifier>();
        services.TryAddSingleton<IAppConfiguration, AppConfiguration>();
        services.TryAddSingleton<IRuntimeEnvironment, RuntimeEnvironment>();
        services.TryAddSingleton<ISecretProtection, SecretProtection>();
        services.TryAddSingleton<IOutboundHttpClients, OutboundHttpClients>();
        services.TryAddSingleton<ITransientState, TransientState>();
        services.TryAddSingleton<IAccessTokenIssuer, AccessTokenIssuer>();
        services.TryAddSingleton<AvatarFileStorage>();
        services.TryAddSingleton<IAvatarStorage>(sp => sp.GetRequiredService<AvatarFileStorage>());
        services.TryAddSingleton<CompanyImageStorage>();
        services.TryAddSingleton<ICompanyImageStorage>(sp => sp.GetRequiredService<CompanyImageStorage>());
        services.TryAddSingleton<IArticleImageStorage, ArticleImageStorage>();
        services.TryAddSingleton<IExternalIdentityClient, ExternalIdentityClient>();
        services.TryAddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
        services.TryAddScoped<IRecoveryEmailSender, SmtpRecoveryEmailSender>();
        return services;
    }
}
