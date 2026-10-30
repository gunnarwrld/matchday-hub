using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MatchdayHub.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthLivenessTests(MatchdayApiFactory factory)
{
    [Fact]
    public async Task Liveness_stays_healthy_even_when_a_readiness_check_fails()
    {
        // Register a "ready"-tagged check that always fails. Readiness must reflect it;
        // liveness must ignore it (otherwise a dependency blip would restart the process).
        using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddHealthChecks().AddCheck(
                    "always-unhealthy",
                    () => HealthCheckResult.Unhealthy("forced failure"),
                    tags: ["ready"])));
        var client = app.CreateClient();

        using var liveness = await client.GetAsync("/health");
        using var readiness = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);
        Assert.Equal("Healthy", await liveness.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
    }
}
