using MatchdayHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MatchdayHub.UnitTests.Support;

/// <summary>
/// A throwaway SQLite database kept entirely in memory, so persistence-level logic
/// (like the ingestion processor's idempotency guards and unique indexes) can be tested
/// without Docker. The single connection stays open for the lifetime of the harness;
/// each <see cref="NewContext"/> is a fresh context over the same database, modelling the
/// per-message scopes the real consumer creates.
/// </summary>
public sealed class SqliteHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<MatchdayDbContext> _options;

    public SqliteHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<MatchdayDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    public MatchdayDbContext NewContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
