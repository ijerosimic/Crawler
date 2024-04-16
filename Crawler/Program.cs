using Crawler;
using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(b => b.AddConsole());
var logger = factory.CreateLogger("ℹ️");

var app = new App(logger);
await app.ProcessAsync();