using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CrawlerTests;

[CollectionDefinition(CrawlerTestCollection)]
public class TestCollection : ICollectionFixture<Fixture>
{
    public const string CrawlerTestCollection = nameof(CrawlerTestCollection);
}

public class Fixture : IAsyncLifetime
{
    private readonly WireMockServer _server = WireMockServer.Start(5226);

    public Task InitializeAsync()
    {
        _server.Given(Request.Create().WithPath("/about").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "html")
                    .WithBody(@"
                        <html>
                            <body>
                                <a href='https://localhost:5226/more'>More</a>
                                <a href='https://localhost:5226/even-more'>Even More</a>
                                <a href='https://localhost:5226/even-more-more'>Even More More</a>
                                <a href='https://localhost:5226/even-even-more-more'>Even Even More More</a>
                            </body>
                        </html>"));

        _server.Given(Request.Create().WithPath("/duplicates-and-junk").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "html")
                    .WithBody(@"
                        <html>
                            <body>
                                <a href='https://localhost:5226/more'>More</a>
                                <a href='https://localhost:5226/more'>More</a>
                                <a href='https://localhost:5226/more'>More</a>
                                <a href='https://localhost:5226/more#Part1'>Part 1</a>
                                <a href='https://localhost:5226/more#Part2'>Part 2</a>
                            </body>
                        </html>"));

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _server.Stop();
        return Task.CompletedTask;
    }
}