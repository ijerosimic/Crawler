using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Crawler;

public class Producer(ILogger logger, IParserService parserServiceService)
{
    private const int DepthLimit = 2;
 
    private record Page(string Url, int Depth);
    
    private readonly ConcurrentDictionary<string, HashSet<string>> _visitedPages = new();
    private readonly Channel<Page> _processingQueue = Channel.CreateUnbounded<Page>();

    public async Task<ConcurrentDictionary<string, HashSet<string>>> Produce(string baseUrl)
    {
        await _processingQueue.Writer.WriteAsync(new Page(baseUrl, 0));
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
                    _processingQueue.Writer.TryComplete();
            });
        }
        catch (Exception e)
        {
            if (e is ChannelClosedException)
                logger.LogWarning("🟡Channel closed");
            else
                logger.LogError(e, "Unhandled exception during processing");
        }

        if (!_processingQueue.Reader.Completion.IsCompleted)
            _processingQueue.Writer.TryComplete();

        logger.LogWarning("🏁Processing complete 🏁");
        return _visitedPages;
    }

    private async Task ProcessAsync(Page page)
    {
        if (page.Depth >= DepthLimit)
        {
            logger.LogDebug("Depth limit reached.");
            return;
        }

        var url = page.Url;
        if (_visitedPages.ContainsKey(url))
        {
            logger.LogDebug("Duplicate key. Skipping.");
            return;
        }

        var links = await parserServiceService.GetLinksAsync(new Uri(url));
        foreach (var link in links)
            _processingQueue.Writer.TryWrite(new Page(link, page.Depth + 1));

        _visitedPages[url] = [..links];
    }
}