using System.ComponentModel.DataAnnotations;

namespace JobTot.Application.Cvs;

// Coordinates and dimensions are millimetres on an A4 page, independent of preview scale.
public sealed class CvDecorationImage
{
    public Guid Id { get; set; }
    [Required, StringLength(200)]
    public string Url { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int Page { get; set; } = 1;
    public double Opacity { get; set; } = 1;
    // Negative layers are behind content; positive layers are in front.
    public int ZIndex { get; set; } = -1;
}

public interface ICvDecorationStorage
{
    string DirectoryPath { get; }
    ValueTask<IAsyncDisposable> LockAsync(CancellationToken ct);
    Task<CvDecorationImage> UploadAsync(Guid accountId, Guid? documentId, bool template,
        byte[] content, string fileName, string contentType, int currentCount, CancellationToken ct);
    Task DeleteUploadAsync(Guid accountId, Guid? documentId, bool template, Guid imageId, CancellationToken ct);
    Task ValidateAsync(CvDocument document, CvDocument? previous, Guid accountId,
        Guid? documentId, bool template, CancellationToken ct);
    Task CleanupAsync(IEnumerable<Guid> imageIds, CancellationToken ct);
}
