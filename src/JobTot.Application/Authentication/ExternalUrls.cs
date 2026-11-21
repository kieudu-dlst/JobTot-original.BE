namespace JobTot.Application.Authentication;

internal static class ExternalUrls
{
    public static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string AddQueryString(string url, IDictionary<string, string?> values)
    {
        var hash = url.IndexOf('#');
        var fragment = hash < 0 ? "" : url[hash..];
        if (hash >= 0)
            url = url[..hash];
        foreach (var pair in values.Where(x => x.Value is not null))
            url += (url.Contains('?') ? "&" : "?") + Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value!);
        return url + fragment;
    }
}
