using Crawler;
using Microsoft.Extensions.Logging;
using Moq;

namespace CrawlerTests;

public class ProducerTests
{
    private const string BaseUrl = "https://www.monzo.com";
    private const string FirstLevelChild = "https://www.monzo.com/about";
    private const string SecondLevelChild = "https://www.monzo.com/features";

    private readonly HashSet<string> _firstLevelUrls = [FirstLevelChild];
    private readonly HashSet<string> _secondLevelUrls = [SecondLevelChild];

    private readonly Mock<IParserService> _mockParser = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public ProducerTests()
    {
        _mockParser.Setup(x => x.GetLinksAsync(new Uri(BaseUrl)))
            .ReturnsAsync([_firstLevelUrls.First()]);

        _mockParser.Setup(x => x.GetLinksAsync(new Uri(_firstLevelUrls.First())))
            .ReturnsAsync([_secondLevelUrls.First()]);
    }

    [Fact]
    public async Task It_Produces_The_Correct_Result()
    {
        var mockLogger = new Mock<ILogger>();

        var sut = new Producer(mockLogger.Object, _mockParser.Object);
        var result = await sut.Produce(BaseUrl, 2);

        Assert.Equal(2, result.Keys.Count);

        Assert.True(result.ContainsKey(BaseUrl));
        Assert.True(result.ContainsKey(FirstLevelChild));
        Assert.False(result.ContainsKey(SecondLevelChild));

        Assert.Single(result[BaseUrl]);
        Assert.Single(result[FirstLevelChild]);

        Assert.Equal(_firstLevelUrls, result[BaseUrl]);
        Assert.Equal(_secondLevelUrls, result[FirstLevelChild]);
    }

    [Fact]
    public async Task It_Produces_The_Correct_Result_When_DepthLimit_Is_0()
    {
        var sut = new Producer(_mockLogger.Object, _mockParser.Object);
        var result = await sut.Produce(BaseUrl, 0);

        Assert.Equal(1, result.Keys.Count);
        Assert.True(result.ContainsKey(BaseUrl));
        Assert.Empty(result[BaseUrl]);
    }
}