using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MatchdayHub.Infrastructure.Aggregator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MatchdayHub.UnitTests;

public class HttpAggregatorClientTests
{
    [Fact]
    public async Task GetMatchesAsync_walks_every_page_until_totalPages_is_exhausted()
    {
        const int totalPages = 3;
        const int perPage = 2;
        var handler = new PagingHandler(totalPages, perPage);
        var client = NewClient(handler, maxFixturePages: 50);

        var matches = await client.GetMatchesAsync();

        Assert.Equal(totalPages, handler.RequestedPages.Count);
        Assert.Equal([1, 2, 3], handler.RequestedPages);
        Assert.Equal(totalPages * perPage, matches.Count);
    }

    [Fact]
    public async Task GetMatchesAsync_stops_at_the_max_pages_guard_and_logs_a_warning()
    {
        const int totalPages = 5;
        const int perPage = 2;
        const int maxPages = 2;
        var handler = new PagingHandler(totalPages, perPage);
        var logger = new CapturingLogger();
        var client = NewClient(handler, maxFixturePages: maxPages, logger: logger);

        var matches = await client.GetMatchesAsync();

        Assert.Equal([1, 2], handler.RequestedPages); // guard stops the walk early
        Assert.Equal(maxPages * perPage, matches.Count);
        Assert.Contains(logger.Warnings, w => w.Contains("guard") && w.Contains("totalPages"));
    }

    [Fact]
    public async Task GetMatchesAsync_single_page_makes_one_request()
    {
        var handler = new PagingHandler(totalPages: 1, perPage: 3);
        var client = NewClient(handler, maxFixturePages: 50);

        var matches = await client.GetMatchesAsync();

        Assert.Equal([1], handler.RequestedPages);
        Assert.Equal(3, matches.Count);
    }

    private static HttpAggregatorClient NewClient(
        HttpMessageHandler handler, int maxFixturePages, CapturingLogger? logger = null)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://aggregator.test") };
        var options = Options.Create(new AggregatorOptions { MaxFixturePages = maxFixturePages });
        return new HttpAggregatorClient(httpClient, logger ?? new CapturingLogger(), options);
    }

    /// <summary>Serves the aggregator's paginated `/matches` envelope, one deterministic page at a time.</summary>
    private sealed class PagingHandler(int totalPages, int perPage) : HttpMessageHandler
    {
        public List<int> RequestedPages { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var match = Regex.Match(request.RequestUri!.Query, @"page=(\d+)");
            var page = match.Success ? int.Parse(match.Groups[1].Value) : 1;
            RequestedPages.Add(page);

            var rows = string.Join(",", Enumerable.Range(0, perPage)
                .Select(i => $"{{\"externalId\":{((page - 1) * perPage) + i + 1}}}"));
            var meta = $"{{\"page\":{page},\"limit\":{perPage},\"total\":{totalPages * perPage},\"totalPages\":{totalPages}}}";
            var body = $"{{\"success\":true,\"data\":[{rows}],\"meta\":{meta}}}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class CapturingLogger : ILogger<HttpAggregatorClient>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
