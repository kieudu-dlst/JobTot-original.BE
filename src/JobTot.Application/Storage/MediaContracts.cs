namespace JobTot.Application.Storage;

public sealed record UploadFile(long Length, string FileName, string ContentType,
    Func<Stream, CancellationToken, Task> CopyToAsync);
public interface IAvatarStorage
{
    string DirectoryPath
    {
        get;
    }
    Task<string> SaveAsync(byte[] content, bool png, CancellationToken ct);
    void Delete(string? url);
}
public interface ICompanyImageStorage
{
    string DirectoryPath
    {
        get;
    }
    Task<string> SaveAsync(byte[] content, string kind, bool png, CancellationToken ct);
}
public interface IArticleImageStorage
{
    Task<string> SaveAsync(byte[] content, bool png, CancellationToken ct);
    void Delete(string url);
}
