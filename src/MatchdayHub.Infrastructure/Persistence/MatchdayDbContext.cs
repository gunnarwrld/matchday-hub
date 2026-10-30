using MatchdayHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MatchdayHub.Infrastructure.Persistence;

/// <summary>
/// EF Core context — the Prisma-client equivalent. <see cref="DbSet{TEntity}"/>
/// properties are the typed table accessors; LINQ queries translate to SQL the same
/// way Prisma's query builder does. Mapping lives in <see cref="OnModelCreating"/>.
/// </summary>
public class MatchdayDbContext(DbContextOptions<MatchdayDbContext> options) : DbContext(options)
{
    public DbSet<Competition> Competitions => Set<Competition>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();

    public DbSet<PlayerMatchLoad> PlayerMatchLoads => Set<PlayerMatchLoad>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchdayDbContext).Assembly);
    }
}
