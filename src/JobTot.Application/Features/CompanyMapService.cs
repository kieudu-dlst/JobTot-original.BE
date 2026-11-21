
namespace JobTot.Application.Features;

public sealed class CompanyMapService(
    IOutboundHttpClients clients,
    ICurrentActor actor
) : ApplicationService(actor)
{
    // Never follow arbitrary redirects: only Google Maps hosts may be contacted.
    public static bool IsMapsUrl(Uri uri) =>
        uri.Scheme == "https"
        && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo)
        && (
            uri.Host == "maps.app.goo.gl"
            || uri.Host == "maps.google.com"
            || (
                uri.Host is "www.google.com" or "google.com"
                && uri.AbsolutePath.StartsWith("/maps", StringComparison.Ordinal)
            )
            || (
                uri.Host == "goo.gl"
                && uri.AbsolutePath.StartsWith("/maps/", StringComparison.Ordinal)
            )
        );
    public async Task<UseCaseResult> Resolve(string url, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (
            string.IsNullOrWhiteSpace(url)
            || url.Length > 2048
            || !Uri.TryCreate(url, UriKind.Absolute, out var current)
            || !IsMapsUrl(current)
        )
            return Invalid(new
            {
                detail = "Liên kết Google Maps không hợp lệ."
            });
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var client = clients.CreateClient("company-maps");
            for (var step = 0; step < 5; step++)
            {
                deadline.Token.ThrowIfCancellationRequested();
                if (current.Host is "www.google.com" or "google.com" or "maps.google.com")
                    return Success(new
                    {
                        url = current.AbsoluteUri
                    });
                using var request = new HttpRequestMessage(HttpMethod.Get, current);
                using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    deadline.Token
                );
                deadline.Token.ThrowIfCancellationRequested();
                if ((int)response.StatusCode >= 400)
                    return ExternalFailure(
                        new
                        {
                            detail = "Google Maps không thể xử lý liên kết lúc này. Vui lòng thử lại.",
                        }
                    );
                var location = response.Headers.Location;
                if ((int)response.StatusCode is < 300 or >= 400 || location is null)
                    break;
                var next = location.IsAbsoluteUri ? location : new Uri(current, location);
                if (!IsMapsUrl(next))
                    break;
                current = next;
            }
        }
        catch (OperationCanceledException)
            when (!ct.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return ExternalTimeout(new
            {
                detail = "Google Maps phản hồi chậm. Vui lòng thử lại."
            });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ExternalFailure(
                new
                {
                    detail = "Kết nối Google Maps bị gián đoạn. Vui lòng thử lại."
                }
            );
        }
        catch (HttpRequestException)
        {
            ct.ThrowIfCancellationRequested();
            return ExternalFailure(
                new
                {
                    detail = "Không thể mở liên kết Google Maps. Vui lòng thử lại."
                }
            );
        }
        return Unprocessable(
            new
            {
                detail = "Không thể đọc vị trí từ liên kết này. Hãy dùng link Google Maps đầy đủ hoặc địa chỉ.",
            }
        );
    }
    public Task<UseCaseResult> ResolvePublic(string url, CancellationToken ct) =>
        Resolve(url, ct);
}
