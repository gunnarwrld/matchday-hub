using MatchdayHub.Domain.Entities;
using MatchdayHub.Infrastructure.Persistence;
using MatchdayHub.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MatchdayHub.Infrastructure.Ingestion;

/// <summary>Outcome of processing a single ingestion message.</summary>
public enum IngestionResult
{
    /// <summary>The event was new and has been persisted.</summary>
    Processed,

    /// <summary>The message had already been processed; this was a no-op.</summary>
    Skipped,

    /// <summary>The referenced match or team is unknown, so nothing was written.</summary>
    Unresolved,
}

/// <summary>
/// The idempotent heart of ingestion. Two independent guards make re-delivery a no-op:
/// a check against the <see cref="ProcessedMessage"/> ledger, and a unique index on
/// <see cref="MatchEvent.UpstreamEventId"/> that turns a racing duplicate insert into a
/// swallowed <see cref="DbUpdateException"/>. This is what the idempotency unit test
/// drives directly.
/// </summary>
public sealed class IngestionProcessor(
    MatchdayDbContext db,
    IMatchNotifier notifier,
    ILogger<IngestionProcessor> logger)
{
    public async Task<IngestionResult> ProcessAsync(LiveEventMessage message, CancellationToken cancellationToken = default)
    {
        // Guard 1 — the processed-message ledger (covers any message, not just events).
        if (await db.ProcessedMessages.AnyAsync(p => p.MessageId == message.MessageId, cancellationToken))
        {
            return IngestionResult.Skipped;
        }

        var match = await db.Matches
            .FirstOrDefaultAsync(m => m.ExternalId == message.MatchExternalId, cancellationToken);
        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.ExternalId == message.TeamExternalId, cancellationToken);

        if (match is null || team is null)
        {
            logger.LogWarning(
                "Cannot ingest event {MessageId}: match {MatchExt} or team {TeamExt} not found.",
                message.MessageId, message.MatchExternalId, message.TeamExternalId);
            return IngestionResult.Unresolved;
        }

        // Guard 2 — the natural key. Also protects against a ledger/event divergence.
        var alreadyStored = await db.MatchEvents
            .AnyAsync(e => e.UpstreamEventId == message.MessageId, cancellationToken);
        if (!alreadyStored)
        {
            db.MatchEvents.Add(new MatchEvent
            {
                UpstreamEventId = message.MessageId,
                MatchId = match.Id,
                TeamId = team.Id,
                PlayerName = message.PlayerName,
                Type = message.Type,
                Minute = message.Minute,
            });
        }

        match.HomeScore = message.HomeScore;
        match.AwayScore = message.AwayScore;
        match.Status = message.MatchStatus;

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = message.MessageId,
            MessageType = nameof(LiveEventMessage),
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // A concurrent delivery won the race on one of the unique indexes.
            logger.LogDebug(ex, "Duplicate ingestion for {MessageId} resolved as skip.", message.MessageId);
            return IngestionResult.Skipped;
        }

        await notifier.MatchEventAsync(
            new MatchEventBroadcast(
                match.Id, message.Type.ToString(), message.Minute, message.PlayerName,
                team.Id, match.HomeScore, match.AwayScore),
            cancellationToken);

        await notifier.ScoreUpdateAsync(
            new ScoreUpdateBroadcast(match.Id, match.HomeScore, match.AwayScore, match.Status.ToString()),
            cancellationToken);

        return IngestionResult.Processed;
    }
}
