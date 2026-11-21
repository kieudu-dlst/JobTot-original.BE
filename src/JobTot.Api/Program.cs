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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, CurrentActor>();
builder.Services.AddApplication();

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

builder.Services.AddHttpClient("external-auth", client => client.Timeout = TimeSpan.FromSeconds(20))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is required.")
);
var app = builder.Build();
if (args.Contains("--migrate-avatars"))
{
    using var scope = app.Services.CreateScope();
    var count = await scope.ServiceProvider.GetRequiredService<AvatarMigration>()
        .RunAsync(app.Lifetime.ApplicationStopping);
    app.Logger.LogInformation("Migrated {Count} avatars to backend files.", count);
    await app.DisposeAsync();
    return;
}

// Explicit one-shot provisioning, never grant admin privileges from a public registration request.
if (args.Contains("--create-admin") || args.Contains("--create-cv-admin"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AdminProvisioning>().CreateAsync(
        Environment.GetEnvironmentVariable("JOBTOT_ADMIN_EMAIL"),
        Environment.GetEnvironmentVariable("JOBTOT_ADMIN_PASSWORD"), app.Lifetime.ApplicationStopping);
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

using var decorationScope = app.Services.CreateScope();
var decorationFiles = decorationScope.ServiceProvider.GetRequiredService<JobTot.Application.Cvs.ICvDecorationStorage>();
Directory.CreateDirectory(decorationFiles.DirectoryPath);
using var decorationProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(decorationFiles.DirectoryPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = decorationProvider,
    RequestPath = "/uploads/cv-decorations",
    OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff",
});

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
