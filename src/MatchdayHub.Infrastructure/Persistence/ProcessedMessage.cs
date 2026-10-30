namespace MatchdayHub.Infrastructure.Persistence;

/// <summary>
/// Idempotency ledger. Every ingestion message the consumer processes is recorded
/// here by its upstream id; a message whose id already exists is skipped. This is the
/// "processed-message table" pattern (a.k.a. the inbox) — the same guarantee BullMQ
/// gives via <c>jobId</c> de-duplication, but owned by our own transaction so it is
/// transport-agnostic.
/// </summary>
public class ProcessedMessage
{
    public string MessageId { get; set; } = string.Empty;

    public string MessageType { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
