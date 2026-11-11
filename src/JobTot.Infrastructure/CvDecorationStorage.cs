using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using JobTot.Application;
using JobTot.Application.Cvs;
using JobTot.Domain.Enums;
using JobTot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobTot.Infrastructure;

public sealed class CvDecorationStorage(RecruitmentDbContext db, IWebHostEnvironment environment,
    IConfiguration configuration, ILogger<CvDecorationStorage> logger) : ICvDecorationStorage
{
    public const string UrlPrefix = "/uploads/cv-decorations/";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string Root => Path.GetFullPath(configuration["Media:CvDecorationDirectory"]
        ?? Path.Combine(environment.ContentRootPath, "App_Data", "cv-decorations"));
    public string DirectoryPath => Path.Combine(Root, "files");
    private string MetadataPath(Guid id) => Path.Combine(Root, "metadata", $"{id:N}.json");
    private sealed record Asset(Guid Id, Guid AccountId, Guid? DocumentId, bool Template,
        string Extension, int PixelWidth, int PixelHeight);
    private sealed class Lease(FileStream stream) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => stream.DisposeAsync();
    }
    public async ValueTask<IAsyncDisposable> LockAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(Root);
        // A shared-filesystem lock also protects references across API processes/instances.
        for (var attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return new Lease(new FileStream(Path.Combine(Root, ".assets.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None)); }
            catch (IOException) when (attempt < 600) { await Task.Delay(50, ct); }
        }
    }
    private async Task<string?> AuthorizeAsync(Guid accountId, Guid? documentId, bool template, CancellationToken ct)
    {
        if (template)
        {
            if (!await db.AccountRecords.AnyAsync(a => a.Id == accountId && a.AccountType == "Admin"
                && a.AccountStatus == "Active" && a.Status == EntityStatus.Active, ct))
                throw new NotFoundException("Không tìm thấy mẫu CV.");
            if (documentId is null) return null;
            return await db.CvTemplateRecords.AsNoTracking().Where(t => t.Id == documentId
                    && t.Status == EntityStatus.Active && t.DesignJson != null)
                .Select(t => t.DesignJson).SingleOrDefaultAsync(ct)
                ?? throw new NotFoundException("Không tìm thấy mẫu CV.");
        }
        return await db.CandidateCvRecords.AsNoTracking().Where(c => c.Id == documentId
                && c.Candidate.AccountId == accountId && c.Status == EntityStatus.Active
                && c.Candidate.Status == EntityStatus.Active && c.Candidate.Account.Status == EntityStatus.Active
                && c.Candidate.Account.AccountType == "Candidate" && c.Candidate.Account.AccountStatus == "Active"
                && c.OnlineDocumentJson != null)
            .Select(c => c.OnlineDocumentJson).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Không tìm thấy CV của bạn.");
    }
    private async Task<Asset> ReadAsync(Guid id, CancellationToken ct)
    {
        if (!File.Exists(MetadataPath(id))) throw new ArgumentException("Ảnh trang trí không còn tồn tại. Hãy tải lại ảnh.");
        return JsonSerializer.Deserialize<Asset>(await File.ReadAllTextAsync(MetadataPath(id), ct), Json)
            ?? throw new ArgumentException("Thông tin ảnh trang trí không hợp lệ.");
    }
    public async Task<CvDecorationImage> UploadAsync(Guid accountId, Guid? documentId, bool template,
        byte[] content, string fileName, string contentType, int currentCount, CancellationToken ct)
    {
        await using var lease = await LockAsync(ct);
        await AuthorizeAsync(accountId, documentId, template, ct);
        if (currentCount is < 0 or >= 5) throw new ArgumentException("Mỗi CV có tối đa 5 ảnh trang trí.");
        if (content.Length is <= 0 or > 2 * 1024 * 1024) throw new ArgumentException("Mỗi ảnh trang trí tối đa 2 MB.");
        var (extension, width, height) = Identify(content);
        var requested = Path.GetExtension(fileName).ToLowerInvariant();
        if ((extension == ".jpg" ? requested is not (".jpg" or ".jpeg") : requested != extension)
            || contentType != (extension == ".jpg" ? "image/jpeg" : extension == ".png" ? "image/png" : "image/webp"))
            throw new ArgumentException("Chọn tệp PNG, JPG/JPEG hoặc WebP có nội dung hợp lệ.");
        if (width <= 0 || height <= 0 || width > 8000 || height > 8000 || (long)width * height > 32_000_000)
            throw new ArgumentException("Kích thước ảnh quá lớn hoặc không hợp lệ (tối đa 8000 px và 32 triệu điểm ảnh).");
        var asset = new Asset(Guid.NewGuid(), accountId, documentId, template, extension, width, height);
        Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(Path.Combine(Root, "metadata"));
        var path = Path.Combine(DirectoryPath, $"{asset.Id:N}{extension}");
        try
        {
            await File.WriteAllBytesAsync(path, content, ct);
            await File.WriteAllTextAsync(MetadataPath(asset.Id), JsonSerializer.Serialize(asset, Json), ct);
        }
        catch { File.Delete(path); File.Delete(MetadataPath(asset.Id)); throw; }
        var w = Math.Min(55d, 85d * width / height);
        var h = w * height / width;
        return new CvDecorationImage { Id = asset.Id, Url = UrlPrefix + asset.Id.ToString("N") + extension,
            X = 195 - w, Y = 282 - h, Width = w, Height = h };
    }
    public async Task ValidateAsync(CvDocument document, CvDocument? previous, Guid accountId,
        Guid? documentId, bool template, CancellationToken ct)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(document, new ValidationContext(document), errors, true))
            throw new ArgumentException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
        foreach (var image in document.DecorationImages)
        {
            var asset = await ReadAsync(image.Id, ct);
            var url = UrlPrefix + asset.Id.ToString("N") + asset.Extension;
            var inherited = previous?.DecorationImages.Any(i => i.Id == image.Id && i.Url == url) == true;
            if (image.Url != url || !File.Exists(Path.Combine(DirectoryPath, $"{asset.Id:N}{asset.Extension}"))
                || (!inherited && (asset.AccountId != accountId || asset.Template != template
                    || asset.DocumentId != documentId)))
                throw new ArgumentException("Ảnh trang trí không thuộc CV hoặc mẫu CV này.");
            if (Math.Abs(image.Width / image.Height / ((double)asset.PixelWidth / asset.PixelHeight) - 1) > 0.01)
                throw new ArgumentException("Ảnh trang trí phải giữ nguyên tỷ lệ gốc.");
        }
    }
    public async Task DeleteUploadAsync(Guid accountId, Guid? documentId, bool template, Guid imageId, CancellationToken ct)
    {
        await using var lease = await LockAsync(ct);
        var current = await AuthorizeAsync(accountId, documentId, template, ct);
        // A successful autosave may already have removed an unreferenced asset.
        // Retrying deletion is idempotent, but document authorization is always checked first.
        if (!File.Exists(MetadataPath(imageId))) return;
        var asset = await ReadAsync(imageId, ct);
        var inherited = current is not null && JsonSerializer.Deserialize<CvDocument>(current, Json)!
            .DecorationImages.Any(i => i.Id == imageId);
        if (!inherited && (asset.AccountId != accountId || asset.Template != template || asset.DocumentId != documentId))
            throw new NotFoundException("Không tìm thấy ảnh thuộc CV hoặc mẫu CV này.");
        await CleanupAsync([imageId], ct);
    }
    public async Task CleanupAsync(IEnumerable<Guid> imageIds, CancellationToken ct)
    {
        foreach (var id in imageIds.Distinct())
        {
            try
            {
                if (!File.Exists(MetadataPath(id))) continue;
                var asset = await ReadAsync(id, ct);
                var url = UrlPrefix + id.ToString("N") + asset.Extension;
                // Include archived CVs and submitted snapshots: historical applications must keep their media.
                if (await db.CvTemplateRecords.AnyAsync(t => t.DesignJson != null && t.DesignJson.Contains(url), ct)
                    || await db.CandidateCvRecords.AnyAsync(c => (c.OnlineDocumentJson != null && c.OnlineDocumentJson.Contains(url))
                        || (c.ContentJson != null && c.ContentJson.Contains(url)), ct)
                    || await db.CandidateJobSubmissions.AnyAsync(s => s.CvContentJson.Contains(url), ct)) continue;
                File.Delete(Path.Combine(DirectoryPath, $"{id:N}{asset.Extension}"));
                File.Delete(MetadataPath(id));
            }
            catch (Exception error)
            {
                // The document may already be committed; media cleanup must not turn a successful save into a failed version.
                logger.LogWarning(error, "Could not clean up CV decoration {ImageId}", id);
            }
        }
    }
    private static (string Extension, int Width, int Height) Identify(byte[] bytes)
    {
        var s = bytes.AsSpan();
        if (s.Length >= 45 && s[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && BinaryPrimitives.ReadUInt32BigEndian(s[8..]) == 13 && s.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            var p = 8;
            var data = false;
            while (p <= s.Length - 12)
            {
                var length = BinaryPrimitives.ReadUInt32BigEndian(s[p..]);
                if (length > s.Length - p - 12) break;
                var chunk = s.Slice(p + 4, (int)length + 4);
                uint crc = 0xFFFFFFFF;
                foreach (var value in chunk)
                {
                    crc ^= value;
                    for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
                }
                if (~crc != BinaryPrimitives.ReadUInt32BigEndian(s[(p + 8 + (int)length)..])) break;
                if (chunk[..4].SequenceEqual("IDAT"u8) && length > 0) data = true;
                p += (int)length + 12;
                if (chunk[..4].SequenceEqual("IEND"u8))
                {
                    if (data && length == 0 && p == s.Length)
                        return (".png", BinaryPrimitives.ReadInt32BigEndian(s[16..]), BinaryPrimitives.ReadInt32BigEndian(s[20..]));
                    break;
                }
            }
        }
        if (s.Length >= 25 && s[0] == 255 && s[1] == 216 && s[^2] == 255 && s[^1] == 217)
        {
            var p = 2;
            var width = 0;
            var height = 0;
            while (p + 4 <= s.Length)
            {
                if (s[p++] != 255) break;
                while (p < s.Length && s[p] == 255) p++;
                if (p >= s.Length) break;
                var marker = s[p++];
                if (marker is 0xD8 or 0x01 or >= 0xD0 and <= 0xD7) continue;
                if (p + 2 > s.Length) break;
                var length = BinaryPrimitives.ReadUInt16BigEndian(s[p..]);
                if (length < 2 || p + length > s.Length) break;
                if (marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC) && length >= 8)
                {
                    width = BinaryPrimitives.ReadUInt16BigEndian(s[(p + 5)..]);
                    height = BinaryPrimitives.ReadUInt16BigEndian(s[(p + 3)..]);
                }
                if (marker == 0xDA)
                {
                    if (width > 0 && height > 0 && p + length < s.Length - 2) return (".jpg", width, height);
                    break;
                }
                p += length;
            }
        }
        if (s.Length >= 30 && s[..4].SequenceEqual("RIFF"u8) && s.Slice(8, 4).SequenceEqual("WEBP"u8)
            && BinaryPrimitives.ReadUInt32LittleEndian(s[4..]) == s.Length - 8)
        {
            var validPayload = false;
            var p = 12;
            while (p <= s.Length - 8)
            {
                var length = BinaryPrimitives.ReadUInt32LittleEndian(s[(p + 4)..]);
                if (length > s.Length - p - 8) break;
                if ((s.Slice(p, 4).SequenceEqual("VP8 "u8) && length >= 10)
                    || (s.Slice(p, 4).SequenceEqual("VP8L"u8) && length >= 5)
                    || (s.Slice(p, 4).SequenceEqual("ANMF"u8) && length >= 24)) validPayload = true;
                p += 8 + (int)length + (int)(length & 1);
            }
            if (!validPayload || p != s.Length)
                throw new ArgumentException("Nội dung ảnh WebP không hợp lệ.");
            var chunk = s.Slice(12, 4);
            if (chunk.SequenceEqual("VP8X"u8))
                return (".webp", 1 + s[24] + (s[25] << 8) + (s[26] << 16), 1 + s[27] + (s[28] << 8) + (s[29] << 16));
            if (chunk.SequenceEqual("VP8L"u8) && s[20] == 0x2F)
                return (".webp", 1 + s[21] + ((s[22] & 0x3F) << 8), 1 + (s[22] >> 6) + (s[23] << 2) + ((s[24] & 0x0F) << 10));
            if (chunk.SequenceEqual("VP8 "u8) && s.Slice(23, 3).SequenceEqual(new byte[] { 0x9D, 0x01, 0x2A }))
                return (".webp", BinaryPrimitives.ReadUInt16LittleEndian(s[26..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(s[28..]) & 0x3FFF);
        }
        throw new ArgumentException("Nội dung ảnh không hợp lệ. Chỉ chấp nhận PNG, JPG/JPEG hoặc WebP.");
    }
}
