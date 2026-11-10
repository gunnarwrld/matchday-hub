namespace MatchdayHub.IntegrationTests;

/// <summary>
/// Shares one <see cref="MatchdayApiFactory"/> (and therefore one Postgres container)
/// across the integration tests, so the container starts once per run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<MatchdayApiFactory>
{
    public const string Name = "api";
}
