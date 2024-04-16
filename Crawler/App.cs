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
                logger.LogDebug("Processing {Url}", url);
                await ProcessAsync(url);
                if (reader.Completion.IsCompleted || token.IsCancellationRequested || reader.Count == 0)
                {
                    logger.LogDebug("Reader completed");
                    _processingQueue.Writer.Complete();
                }
            }).ContinueWith(task =>
            {
                if (task.IsFaulted && task.Exception.InnerException is ChannelClosedException)
                {
                    logger.LogWarning("🟡Channel closed");
                }
                else if (task.IsFaulted)
                {
                    logger.LogError(task.Exception, "🔴Error processing");
                    throw task.Exception;
                }
            }, token);

            foreach (var (url, links) in _resultDict)
                logger.LogWarning("🟢Url {Url} has {Count} links: 🔗{@Links} 🟢", url, links.Count, links);
        }
        catch (Exception e)
        {
            if (e is ChannelClosedException)
            {
                logger.LogWarning("🟡Channel closed");
            }
            else throw;
        }
        finally
        {
            if (!_processingQueue.Reader.Completion.IsCompleted)
                _processingQueue.Writer.TryComplete();
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
            logger.LogWarning("Duplicate key. Skipping.");
            return;
        }

        var links = await GetLinksAsync(new Uri(url));
        logger.LogDebug("Found {Count} links at {Url}.", links.Count, url);
        foreach (var link in links)
        {
            logger.LogDebug(" 🔗{link}", link);
            _processingQueue.Writer.TryWrite(new Page(link, page.Depth + 1));
        }

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