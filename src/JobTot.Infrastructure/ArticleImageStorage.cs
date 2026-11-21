using JobTot.Application.Storage;
using Microsoft.AspNetCore.Hosting;
namespace JobTot.Infrastructure;

public sealed class ArticleImageStorage(IWebHostEnvironment environment) : IArticleImageStorage
{
    private string Folder => Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "articles");
    public async Task<string> SaveAsync(byte[] content, bool png, CancellationToken ct)
    {
        Directory.CreateDirectory(Folder);
        var name = $"{Guid.NewGuid():N}.{(png ? "png" : "jpg")}";
        var url = "/uploads/articles/" + name;
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(Folder, name), content, ct);
        }
        catch { Delete(url); throw; }
        return url;
    }
    public void Delete(string url)
    {
        var name = Path.GetFileName(url);
        if (name.Length == 36 && Guid.TryParseExact(name[..32], "N", out _) && name[32..] is ".png" or ".jpg")
            File.Delete(Path.Combine(Folder, name));
    }
}
