using Crawler;
using Microsoft.Extensions.Logging;
using Moq;

namespace CrawlerTests;

public class ProducerTests
{
    [Fact]
    public async Task TestProcess()
    {
        const string baseUrl = "https://www.monzo.com";
        var expectedResult = new HashSet<string> { "https://www.monzo.com/about" };
        
        var mockParser = new Mock<IParserService>();
        mockParser.Setup(x => x.GetLinksAsync(new Uri(baseUrl)))
            .ReturnsAsync([expectedResult.First()]);
        var mockLogger = new Mock<ILogger>();
        
        var sut = new Producer(mockLogger.Object, mockParser.Object);
        var result = await sut.Produce(baseUrl);

        Assert.Single(result);
        Assert.Equal(baseUrl, result.First().Key);
        Assert.Single(result.First().Value);
        Assert.Equal(expectedResult, result.First().Value);
    }
}