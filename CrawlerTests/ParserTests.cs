using Crawler;

namespace CrawlerTests;

[Collection(TestCollection.CrawlerTestCollection)]
public class ParserTests
{
    private const string BaseUrl = "http://localhost:5226";
    
    [Fact]
    public async Task ItReturnsExpectedResult()
    {
        var sut = new HtmlWebUrlParser();
        var result = await sut.GetLinksAsync(new Uri(BaseUrl + "/about"));

        Assert.Equal(4, result.Count);
        Assert.Equal("https://localhost:5226/more", result.First());
        Assert.Equal("https://localhost:5226/even-more", result[1]);
        Assert.Equal("https://localhost:5226/even-more-more", result[2]);
        Assert.Equal("https://localhost:5226/even-even-more-more", result[3]);
    }
    
    [Fact]
    public async Task ItReturnsNormalizedResult_When_Data_Contains_Junk()
    {
        var sut = new HtmlWebUrlParser();
        var result = await sut.GetLinksAsync(new Uri(BaseUrl + "/duplicates-and-junk"));

        Assert.Single(result);
        Assert.Equal("https://localhost:5226/more", result.First());
    }
}