using MatchdayHub.Domain.Enums;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>
/// A unit of work on the ingestion queue: one live match event plus the match's
/// current score snapshot. <see cref="MessageId"/> is the upstream event id and the
/// idempotency key.
/// </summary>
public sealed record LiveEventMessage(
    string MessageId,
    int MatchExternalId,
    int TeamExternalId,
    string? PlayerName,
    MatchEventType Type,
    int Minute,
    int HomeScore,
    int AwayScore,
    MatchStatus MatchStatus);
