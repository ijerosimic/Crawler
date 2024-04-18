using Crawler;
using Microsoft.Extensions.Logging;
using Moq;

namespace CrawlerTests;

public class ProducerTests
{
    [Fact]
    public async Task TestProcess()
    {
        var mockParser = new Mock<IParserService>();
        mockParser.Setup(x => x.GetLinksAsync(new Uri("https://www.monzo.com")))
            .ReturnsAsync([
                "https://www.monzo.com/about"
            ]);
        var mockLogger = new Mock<ILogger>();
        
        var sut = new Producer(mockLogger.Object, mockParser.Object);
        var result = await sut.Produce("https://www.monzo.com");

        Assert.Single(result);
        Assert.Equal("https://www.monzo.com", result.First().Key);
        Assert.Single(result.First().Value);
        Assert.Equal("https://www.monzo.com/about", result.First().Value.First());
    }
}