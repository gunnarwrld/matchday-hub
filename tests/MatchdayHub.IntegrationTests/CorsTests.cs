using Microsoft.AspNetCore.Hosting;

namespace MatchdayHub.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CorsTests(MatchdayApiFactory factory)
{
    private const string Allowed = "https://allowed.example";

    [Fact]
    public async Task Preflight_from_an_allowed_origin_is_granted_with_credentials()
    {
        var client = NewClientAllowing(Allowed);

        using var response = await PreflightAsync(client, "/hubs/matches/negotiate", origin: Allowed);

        Assert.Equal(Allowed, Single(response, "Access-Control-Allow-Origin"));
        Assert.Equal("true", Single(response, "Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Preflight_from_a_disallowed_origin_gets_no_cors_headers()
    {
        var client = NewClientAllowing(Allowed);

        using var response = await PreflightAsync(client, "/hubs/matches/negotiate", origin: "https://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    private HttpClient NewClientAllowing(string origin) =>
        factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", origin)).CreateClient();

    private static async Task<HttpResponseMessage> PreflightAsync(HttpClient client, string path, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        return await client.SendAsync(request);
    }

    private static string? Single(HttpResponseMessage response, string header) =>
        response.Headers.TryGetValues(header, out var values) ? values.Single() : null;
}
