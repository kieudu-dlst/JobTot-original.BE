namespace JobTot.Api;

public sealed class AvatarFileStorage(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<AvatarFileStorage> logger
)
{
    public const string UrlPrefix = "/uploads/avatars/";
    public string DirectoryPath { get; } =
        Path.GetFullPath(
            configuration["Media:AvatarDirectory"]
                ?? Path.Combine(
                    environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
                    "uploads",
                    "avatars"
                )
        );

    public async Task<string> SaveAsync(byte[] content, bool png, CancellationToken ct)
    {
        Directory.CreateDirectory(DirectoryPath);
        var name = $"{Guid.NewGuid():N}.{(png ? "png" : "jpg")}";
        var url = UrlPrefix + name;
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(DirectoryPath, name), content, ct);
            return url;
        }
        catch
        {
            // This file has not been referenced by a database write yet.
            Delete(url);
            throw;
        }
    }

    public void Delete(string? url)
    {
        if (url is null || !url.StartsWith(UrlPrefix, StringComparison.Ordinal))
            return;
        var name = url[UrlPrefix.Length..];
        if (
            name.Length != 36
            || !Guid.TryParseExact(name[..32], "N", out _)
            || (name[32..] != ".png" && name[32..] != ".jpg")
        )
            return;
        try
        {
            File.Delete(Path.Combine(DirectoryPath, name));
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Could not remove obsolete avatar {Name}", name);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Could not remove obsolete avatar {Name}", name);
        }
    }
}
