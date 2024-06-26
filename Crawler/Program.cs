﻿using Crawler;
using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(b => b.AddConsole());
var logger = factory.CreateLogger("ℹ️");

const string baseUrl = "https://www.monzo.com";
const int depthLimit = 2;

var parser = new HtmlWebUrlParser();
var app = new Producer(logger, parser);
var results = await app.Produce(baseUrl, depthLimit);

logger.LogWarning("Found {Count} links at {BaseUrl}", results.Count, baseUrl);
foreach (var (page, links) in results)
    logger.LogWarning("🟢Url {Url} has {Count} links: 🔗{@Links} 🟢", page, links.Count, links);