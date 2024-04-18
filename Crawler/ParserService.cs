using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Crawler;

public interface IParserService
{
    Task<List<string>> GetLinksAsync(Uri uri);
}

public class HtmlWebUrlParser : IParserService
{
    public async Task<List<string>> GetLinksAsync(Uri uri)
    {
        var doc = await new HtmlWeb().LoadFromWebAsync(uri.OriginalString);
        var regex = new Regex("^http(s)?://" + uri.Host, RegexOptions.IgnoreCase);

        return doc.DocumentNode
            .Descendants("a")
            .Select(a =>
            {
                var val = a.GetAttributeValue("href", string.Empty);
                val = val.StartsWith('/') ? uri.GetLeftPart(UriPartial.Authority) + val : val;
                return val.NormalizeUrl();
            })
            .Distinct()
            .Where(u => !string.IsNullOrEmpty(u) && regex.IsMatch(u) && u != uri.ToString() && u != uri.OriginalString)
            .ToList();
    }
}