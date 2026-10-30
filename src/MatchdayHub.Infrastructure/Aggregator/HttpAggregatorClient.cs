using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MatchdayHub.Infrastructure.Aggregator;

/// <summary>
/// Typed <c>HttpClient</c> against the real aggregator. Registered with
/// <c>AddStandardResilienceHandler</c> (retries, circuit breaker, timeouts) — the
/// .NET counterpart of wrapping <c>fetch</c> in a retry/backoff policy.
/// The base address and default headers are configured at registration time.
/// </summary>
public sealed class HttpAggregatorClient(HttpClient httpClient, ILogger<HttpAggregatorClient> logger)
    : IAggregatorClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<AggregatorMatch>> GetMatchesAsync(CancellationToken cancellationToken = default)
    {
        var wire = await GetEnvelopeAsync<List<WireMatch>>(
            "/api/v1/matches?limit=100", cancellationToken);

        return (wire ?? [])
            .Where(m => m.ExternalId is not null)
            .Select(ToAggregatorMatch)
            .ToList();
    }

    public async Task<IReadOnlyList<AggregatorEvent>> GetLiveEventsAsync(CancellationToken cancellationToken = default)
    {
        var wire = await GetEnvelopeAsync<List<WireMatch>>(
            "/api/v1/matches/live", cancellationToken);

        var events = new List<AggregatorEvent>();
        foreach (var match in wire ?? [])
        {
            if (match.ExternalId is null || match.Events is null)
            {
                continue;
            }

            // Map the event's team uuid back to the externalId we key on.
            var teamByUuid = new Dictionary<string, int?>(StringComparer.Ordinal);
            if (match.HomeTeam is { } home)
            {
                teamByUuid[match.HomeTeamId ?? string.Empty] = home.ExternalId;
            }

            if (match.AwayTeam is { } away)
            {
                teamByUuid[match.AwayTeamId ?? string.Empty] = away.ExternalId;
            }

            foreach (var e in match.Events)
            {
                if (e.Id is null || e.TeamId is null ||
                    !teamByUuid.TryGetValue(e.TeamId, out var teamExternalId) || teamExternalId is null)
                {
                    continue;
                }

                events.Add(new AggregatorEvent(
                    UpstreamEventId: e.Id,
                    MatchExternalId: match.ExternalId.Value,
                    TeamExternalId: teamExternalId.Value,
                    PlayerName: e.PlayerName,
                    Type: e.EventType ?? "UNKNOWN",
                    Minute: e.Minute,
                    HomeScore: match.HomeScore,
                    AwayScore: match.AwayScore,
                    MatchStatus: match.Status ?? "LIVE"));
            }
        }

        return events;
    }

    private async Task<T?> GetEnvelopeAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = await httpClient.GetFromJsonAsync<Envelope<T>>(path, JsonOptions, cancellationToken);
            return envelope is null ? default : envelope.Data;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Aggregator request to {Path} failed; treating as empty.", path);
            return default;
        }
    }

    private static AggregatorMatch ToAggregatorMatch(WireMatch m) => new(
        ExternalId: m.ExternalId!.Value,
        CompetitionExternalId: m.League?.ExternalId ?? 0,
        CompetitionName: m.League?.Name ?? "Unknown Competition",
        Country: m.League?.Country ?? "Unknown",
        Season: m.League?.Season ?? "n/a",
        Home: ToTeamRef(m.HomeTeam),
        Away: ToTeamRef(m.AwayTeam),
        KickoffUtc: m.MatchDate ?? DateTimeOffset.UtcNow,
        Status: m.Status ?? "SCHEDULED",
        HomeScore: m.HomeScore,
        AwayScore: m.AwayScore);

    private static AggregatorTeamRef ToTeamRef(WireTeam? team)
    {
        var name = team?.Name ?? "Unknown";
        var shortName = team?.ShortName;
        if (string.IsNullOrWhiteSpace(shortName))
        {
            shortName = name[..Math.Min(3, name.Length)].ToUpperInvariant();
        }

        return new AggregatorTeamRef(team?.ExternalId ?? 0, name, shortName, team?.Stadium ?? string.Empty);
    }

    // ── wire DTOs (the aggregator's JSON shape) ──────────────────────────────

    /// <summary>The aggregator wraps every payload in <c>{ success, data, meta }</c>.</summary>
    private sealed record Envelope<T>([property: JsonPropertyName("data")] T? Data);

    private sealed record WireTeam(
        [property: JsonPropertyName("externalId")] int? ExternalId,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("shortName")] string? ShortName,
        [property: JsonPropertyName("stadium")] string? Stadium);

    private sealed record WireLeague(
        [property: JsonPropertyName("externalId")] int? ExternalId,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("country")] string? Country,
        [property: JsonPropertyName("season")] string? Season);

    private sealed record WireEvent(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("teamId")] string? TeamId,
        [property: JsonPropertyName("playerName")] string? PlayerName,
        [property: JsonPropertyName("eventType")] string? EventType,
        [property: JsonPropertyName("minute")] int Minute);

    private sealed record WireMatch(
        [property: JsonPropertyName("externalId")] int? ExternalId,
        [property: JsonPropertyName("homeTeamId")] string? HomeTeamId,
        [property: JsonPropertyName("awayTeamId")] string? AwayTeamId,
        [property: JsonPropertyName("matchDate")] DateTimeOffset? MatchDate,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("homeScore")] int HomeScore,
        [property: JsonPropertyName("awayScore")] int AwayScore,
        [property: JsonPropertyName("homeTeam")] WireTeam? HomeTeam,
        [property: JsonPropertyName("awayTeam")] WireTeam? AwayTeam,
        [property: JsonPropertyName("league")] WireLeague? League,
        [property: JsonPropertyName("events")] List<WireEvent>? Events);
}
