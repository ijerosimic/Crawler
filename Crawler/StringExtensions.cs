namespace Crawler;

public static class StringExtensions
{
    public static string NormalizeUrl(this string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _))
            return string.Empty;

        return url.Split('?', '#')[0].TrimEnd('/');
    }
}