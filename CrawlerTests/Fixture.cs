using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CrawlerTests;

public class Fixture
{
    internal static void StartServer()
    {
        var server = WireMockServer.Start(5226);
        server.Given(Request.Create().WithPath("/about").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "html")
                    .WithBody(@"
                        <html>
                            <body>
                                <a href='https://localhost:5226/more'>More</a>
                            </body>
                        </html>"));
    }
}