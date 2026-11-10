namespace JobTot.Api;

public sealed class CompanyImageStorage(
    IWebHostEnvironment environment,
    IConfiguration configuration
)
{
    public const string UrlPrefix = "/uploads/companies/";
    public string DirectoryPath { get; } =
        Path.GetFullPath(
            configuration["Media:CompanyDirectory"]
                ?? Path.Combine(
                    environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
                    "uploads",
                    "companies"
                )
        );

    public async Task<string> SaveAsync(byte[] content, string kind, bool png, CancellationToken ct)
    {
        if (kind is not ("logo" or "cover" or "gallery"))
            throw new ArgumentException("Invalid company image kind.");
        var folder =
            kind == "logo" ? "logos"
            : kind == "cover" ? "covers"
            : "gallery";
        var directory = Path.Combine(DirectoryPath, folder);
        Directory.CreateDirectory(directory);
        var name = $"{Guid.NewGuid():N}.{(png ? "png" : "jpg")}";
        var path = Path.Combine(directory, name);
        try
        {
            await File.WriteAllBytesAsync(path, content, ct);
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        return UrlPrefix + folder + "/" + name;
    }
}
