using JobTot.Api;
using JobTot.Application;
using JobTot.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers();
builder
    .Services.AddHttpClient("company-maps", client => client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(
        () => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }
    );
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AvatarFileStorage>();
builder.Services.AddSingleton<CompanyImageStorage>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            };
            return Task.CompletedTask;
        }
    );
    options.AddOperationTransformer(
        (operation, context, ct) =>
        {
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            if (
                metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any()
            )
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer",
                                },
                            }
                        ] = [],
                    },
                ];
            return Task.CompletedTask;
        }
    );
});
builder.Services.AddCandidateAuthentication(
    builder.Configuration,
    builder.Environment.IsDevelopment()
);
builder.Services.AddAdminAuthentication();
builder.Services.AddEmployerAuthentication();
builder.Services.AddScoped<EmployerPasswordRecovery>();
builder.Services.AddScoped<PasswordRecovery>();
builder.Services.AddScoped<AccountEmailSecurity>();
builder.Services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
builder.Services.AddScoped<IRecoveryEmailSender, SmtpRecoveryEmailSender>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddScoped<RecruitmentService>();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is required.")
);
var app = builder.Build();
if (args.Contains("--migrate-avatars"))
{
    using var scope = app.Services.CreateScope();
    var count = await AvatarMigration.RunAsync(
        scope.ServiceProvider.GetRequiredService<JobTot.Infrastructure.Persistence.RecruitmentDbContext>(),
        app.Services.GetRequiredService<AvatarFileStorage>(),
        app.Lifetime.ApplicationStopping
    );
    app.Logger.LogInformation("Migrated {Count} avatars to backend files.", count);
    await app.DisposeAsync();
    return;
}

// Explicit one-shot provisioning, never grant admin privileges from a public registration request.
if (args.Contains("--create-admin") || args.Contains("--create-cv-admin"))
{
    using var scope = app.Services.CreateScope();
    var email = Environment.GetEnvironmentVariable("JOBTOT_ADMIN_EMAIL")?.Trim().ToLowerInvariant();
    var password = Environment.GetEnvironmentVariable("JOBTOT_ADMIN_PASSWORD");
    if (string.IsNullOrWhiteSpace(email) || password is null || password.Length < 12)
        throw new InvalidOperationException(
            "Set JOBTOT_ADMIN_EMAIL and JOBTOT_ADMIN_PASSWORD (at least 12 characters)."
        );
    var db =
        scope.ServiceProvider.GetRequiredService<JobTot.Infrastructure.Persistence.RecruitmentDbContext>();
    if (
        await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            db.AccountRecords,
            a => a.Email == email
        )
    )
        throw new InvalidOperationException("Account already exists; no account was modified.");
    var account = new JobTot.Domain.Entities.Account
    {
        Email = email,
        FullName = "Quản trị viên",
        AccountType = "Admin",
        AccountStatus = "Active",
    };
    account.PasswordHash = scope
        .ServiceProvider.GetRequiredService<JobTot.Application.Authentication.IAccountPasswordHasher>()
        .Hash(account, password);
    db.Add(account);
    await db.SaveChangesAsync();
    await app.DisposeAsync();
    return;
}
app.UseExceptionHandler();
app.UseMiddleware<RequestCancellationMiddleware>();
app.UseStatusCodePages();

// Expose only public media folders; private CV documents continue through authorized APIs.
var avatars = app.Services.GetRequiredService<AvatarFileStorage>();
Directory.CreateDirectory(avatars.DirectoryPath);
using var avatarProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
    avatars.DirectoryPath
);
app.UseStaticFiles(
    new StaticFileOptions { FileProvider = avatarProvider, RequestPath = "/uploads/avatars" }
);
var companyImages = app.Services.GetRequiredService<CompanyImageStorage>();
Directory.CreateDirectory(companyImages.DirectoryPath);
using var companyImageProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
    companyImages.DirectoryPath
);
app.UseStaticFiles(
    new StaticFileOptions
    {
        FileProvider = companyImageProvider,
        RequestPath = "/uploads/companies",
    }
);

// Allow both local launch profiles without redirecting Swagger's fetch to another origin.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("CandidateWeb");
app.UseRateLimiter();
app.UseAuthentication();

// JWT cancellation returns NoResult; stop before authorization can challenge a disconnected client.
app.UseMiddleware<RequestCancellationMiddleware>();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("../openapi/v1.json", "JobTot API v1");
        options.DocumentTitle = "JobTot API";
    });
}
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();
app.Run();

public partial class Program;
