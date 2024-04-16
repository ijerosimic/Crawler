namespace Crawler;

public static class StringExtensions
{
    public static string GetNormalisedUrl(this string domain, string relativePath)
    {
        if (relativePath.StartsWith(domain))
            return relativePath.NormalizeUrl();
        if (relativePath.StartsWith('/') && relativePath.Length > 1)
            return (domain + relativePath).NormalizeUrl();
        return string.Empty;
    }

    public static string NormalizeUrl(this string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _))
            return string.Empty;

        var result = url.Split('?', '#')[0].TrimEnd('/');

        return string
            .Join("/", result
                .Split('/')
                .Select(part =>
                    int.TryParse(part, out _)
                    || Guid.TryParse(part, out _)
                    || DateTime.TryParse(part, out _)
                        ? "0"
                        : part));
    }
}