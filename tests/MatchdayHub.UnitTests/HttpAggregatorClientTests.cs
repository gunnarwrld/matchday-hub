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

    [Fact]
    public async Task GetMatchesAsync_skips_fixtures_with_missing_or_zero_league_and_team_ids()
    {
        const string body = """
            {"success":true,"data":[
              {"externalId":1,"league":{"externalId":10},"homeTeam":{"externalId":20},"awayTeam":{"externalId":30}},
              {"externalId":2,"league":{"externalId":null},"homeTeam":{"externalId":21},"awayTeam":{"externalId":31}},
              {"externalId":3,"league":{"externalId":10},"homeTeam":{"name":"Missing ID"},"awayTeam":{"externalId":33}},
              {"externalId":4,"league":{"externalId":10},"homeTeam":{"externalId":24},"awayTeam":{"externalId":0}}
            ],"meta":{"page":1,"limit":50,"total":4,"totalPages":1}}
            """;
        var logger = new CapturingLogger();
        var client = NewClient(new StaticResponseHandler(body), maxFixturePages: 50, logger);

        var matches = await client.GetMatchesAsync();

        var match = Assert.Single(matches);
        Assert.Equal(1, match.ExternalId);
        Assert.Equal(10, match.CompetitionExternalId);
        Assert.Equal(20, match.Home.ExternalId);
        Assert.Equal(30, match.Away.ExternalId);
        Assert.Equal(3, logger.Warnings.Count);
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
                .Select(i =>
                {
                    var id = ((page - 1) * perPage) + i + 1;
                    return $"{{\"externalId\":{id},\"league\":{{\"externalId\":99,\"name\":\"League\"}}," +
                        $"\"homeTeam\":{{\"externalId\":{1000 + id},\"name\":\"Home\"}}," +
                        $"\"awayTeam\":{{\"externalId\":{2000 + id},\"name\":\"Away\"}}}}";
                }));
            var meta = $"{{\"page\":{page},\"limit\":{perPage},\"total\":{totalPages * perPage},\"totalPages\":{totalPages}}}";
            var body = $"{{\"success\":true,\"data\":[{rows}],\"meta\":{meta}}}";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StaticResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
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
