namespace JobTot.Application.Cvs;

public sealed class CvDecorationService(ICvDecorationStorage storage)
{
    public async Task<CvDecorationImage> UploadAsync(Guid accountId, Guid? id, bool template, UploadFile file, int currentCount, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 2 * 1024 * 1024)
            throw new ArgumentException("Mỗi ảnh trang trí tối đa 2 MB.");
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return await storage.UploadAsync(accountId, id, template, buffer.ToArray(), file.FileName, file.ContentType, currentCount, ct);
    }

    public Task DeleteAsync(Guid accountId, Guid? id, bool template, Guid imageId, CancellationToken ct) =>
        storage.DeleteUploadAsync(accountId, id, template, imageId, ct);
}
