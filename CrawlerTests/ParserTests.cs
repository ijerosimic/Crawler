using Crawler;

namespace CrawlerTests;

public class ParserTests
{
    [Fact]
    public async Task ItReturnsExpectedResult()
    {
        Fixture.StartServer();
        const string baseUrl = "http://localhost:5226/about";
        const string resultUrl = "https://localhost:5226/more";

        var sut = new HtmlWebUrlParser();
        var result = await sut.GetLinksAsync(new Uri(baseUrl));

        Assert.Single(result);
        Assert.Equal(resultUrl, result.First());
    }
}