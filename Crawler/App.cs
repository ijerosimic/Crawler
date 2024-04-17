using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Crawler;

public class App(ILogger logger)
{
    private const string BaseUrl = "https://www.monzo.com";
    private const int DepthLimit = 2;

    private readonly ConcurrentDictionary<string, HashSet<string>> _resultDict = new();
    private readonly Channel<Page> _processingQueue = Channel.CreateUnbounded<Page>();

    public async Task Process()
    {
        await _processingQueue.Writer.WriteAsync(new Page(BaseUrl, 0));
        var reader = _processingQueue.Reader;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
        var token = cts.Token;
        var opts = new ParallelOptions { CancellationToken = token };

        try
        {
            await Parallel.ForEachAsync(reader.ReadAllAsync(token), opts, async (url, _) =>
            {
                await ProcessAsync(url);
                if (reader.Completion.IsCompleted || token.IsCancellationRequested || reader.Count == 0)
                    _processingQueue.Writer.Complete();
            });
        }
        catch (Exception e)
        {
            if (e is ChannelClosedException)
                logger.LogWarning("🟡Channel closed");
            else
                logger.LogError(e, "Unhandled exception during processing");
        }
        finally
        {
            if (!_processingQueue.Reader.Completion.IsCompleted)
                _processingQueue.Writer.TryComplete();
            
            foreach (var (url, links) in _resultDict)
                logger.LogWarning("🟢Url {Url} has {Count} links: 🔗{@Links} 🟢", url, links.Count, links);
            
            logger.LogWarning("🏁Processing complete 🏁");
        }
    }

    private async Task ProcessAsync(Page page)
    {
        if (page.Depth >= DepthLimit)
        {
            logger.LogDebug("Depth limit reached.");
            return;
        }

        var url = page.Url;
        if (_resultDict.ContainsKey(url))
        {
            logger.LogDebug("Duplicate key. Skipping.");
            return;
        }

        var links = await GetLinksAsync(new Uri(url));
        foreach (var link in links)
            _processingQueue.Writer.TryWrite(new Page(link, page.Depth + 1));

        _resultDict[url] = [..links];
    }

    private static async Task<List<string>> GetLinksAsync(Uri uri)
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

public record Page(string Url, int Depth);