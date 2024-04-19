using Crawler;

namespace CrawlerTests;

public class ParserTests
{
    [Fact]
    public async Task ItReturnsExpectedResult()
    {
        Fixture.StartServer();
        const string baseUrl = "http://localhost:5226/about";

        var sut = new HtmlWebUrlParser();
        var result = await sut.GetLinksAsync(new Uri(baseUrl));

        Assert.Equal(4, result.Count);
        Assert.Equal("https://localhost:5226/more", result.First());
        Assert.Equal("https://localhost:5226/even-more", result[1]);
        Assert.Equal("https://localhost:5226/even-more-more", result[2]);
        Assert.Equal("https://localhost:5226/even-even-more-more", result[3]);
    }
    
    [Fact]
    public async Task ItReturnsNormalizedResult_When_Data_Contains_Junk()
    {
        Fixture.StartServer();
        const string baseUrl = "http://localhost:5226/duplicates-and-junk";

        var sut = new HtmlWebUrlParser();
        var result = await sut.GetLinksAsync(new Uri(baseUrl));

        Assert.Single(result);
        Assert.Equal("https://localhost:5226/more", result.First());
    }
}