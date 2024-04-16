using Crawler;
using Microsoft.Extensions.Logging.Abstractions;

namespace CrawlerTests;

public class AppTests
{
    [Fact]
    public async Task TestProcess()
    {
        var app = new App(NullLogger.Instance);
        await app.Process();
    }
}