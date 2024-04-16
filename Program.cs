using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Crawler;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(b => b.AddConsole());
var logger = factory.CreateLogger("ℹ️");

const string baseUrl = "https://www.monzo.com";
const int depthLimit = 2;

var processingQueue = Channel.CreateUnbounded<Page>();
await processingQueue.Writer.WriteAsync(new Page(baseUrl, 0));

var resultDict = new ConcurrentDictionary<string, HashSet<string>>();
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
var token = cts.Token;
var opts = new ParallelOptions { CancellationToken = token };
var reader = processingQueue.Reader;

try
{
    await Parallel.ForEachAsync(reader.ReadAllAsync(token), opts, async (url, _) =>
    {
        logger.LogDebug("Processing {Url}", url);
        await ProcessAsync(url);
        if (reader.Completion.IsCompleted || token.IsCancellationRequested || reader.Count == 0)
        {
            logger.LogDebug("Reader completed");
            processingQueue.Writer.Complete();
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
    });

    foreach (var (url, links) in resultDict)
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
    if (!processingQueue.Reader.Completion.IsCompleted)
        processingQueue.Writer.TryComplete();
    logger.LogWarning("🏁Processing complete 🏁");
}

return;

async Task ProcessAsync(Page page)
{
    if (page.Depth >= depthLimit)
    {
        logger.LogDebug("Depth limit reached.");
        return;
    }

    var url = page.Url;
    if (resultDict.ContainsKey(url))
    {
        logger.LogWarning("Duplicate key. Skipping.");
        return;
    }

    var links = await GetLinksAsync(new Uri(url));
    logger.LogDebug("Found {Count} links at {Url}.", links.Count, url);
    foreach (var link in links)
    {
        logger.LogDebug(" 🔗{link}", link);
        processingQueue.Writer.TryWrite(new Page(link, page.Depth + 1));
    }

    resultDict[url] = [..links];
}

static async Task<List<string>> GetLinksAsync(Uri uri)
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

public record Page(string Url, int Depth);