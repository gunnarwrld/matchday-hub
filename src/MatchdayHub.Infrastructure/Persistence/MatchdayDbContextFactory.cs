using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MatchdayHub.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by the <c>dotnet ef</c> tooling to build the context without
/// booting the full application host. The connection string here is only used for
/// scaffolding/migration generation; the real one is supplied by configuration at runtime.
/// </summary>
public sealed class MatchdayDbContextFactory : IDesignTimeDbContextFactory<MatchdayDbContext>
{
    public MatchdayDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=matchday;Username=matchday;Password=matchday";

        var options = new DbContextOptionsBuilder<MatchdayDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(MatchdayDbContext).Assembly.FullName))
            .Options;

        return new MatchdayDbContext(options);
    }
}
