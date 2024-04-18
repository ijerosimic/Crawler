using Crawler;
using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(b => b.AddConsole());
var logger = factory.CreateLogger("ℹ️");

const string baseUrl = "https://www.monzo.com";

var parser = new HtmlWebUrlParser();
var app = new Producer(logger, parser);
var results = await app.Produce(baseUrl);
foreach (var (url, links) in results)
    logger.LogWarning("🟢Url {Url} has {Count} links: 🔗{@Links} 🟢", url, links.Count, links);