using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MatchdayHub.IntegrationTests;

[Collection(ApiCollection.Name)]
public class MatchEndpointsTests(MatchdayApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ListMatches_returns_a_paged_envelope_with_seeded_rows()
    {
        using var response = await _client.GetAsync("/v1/matches?pageSize=5");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.True(root.GetProperty("total").GetInt32() > 0);
        Assert.Equal(5, root.GetProperty("pageSize").GetInt32());
        Assert.True(root.GetProperty("items").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Filtering_by_status_returns_only_matching_rows()
    {
        using var response = await _client.GetAsync("/v1/matches?status=Finished&pageSize=100");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var item in json.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.Equal("Finished", item.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Invalid_status_filter_is_a_validation_problem()
    {
        using var response = await _client.GetAsync("/v1/matches?status=banana");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetMatch_by_id_returns_detail_with_events()
    {
        var match = await FirstMatchAsync();
        var id = match.GetProperty("id").GetString();

        using var response = await _client.GetAsync($"/v1/matches/{id}");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, json.RootElement.GetProperty("id").GetString());
        Assert.True(json.RootElement.TryGetProperty("events", out _));
    }

    [Fact]
    public async Task GetMatch_with_unknown_id_is_a_404_problem()
    {
        using var response = await _client.GetAsync($"/v1/matches/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TeamForm_returns_computed_form_for_a_seeded_team()
    {
        var match = await FirstMatchAsync();
        var teamId = match.GetProperty("home").GetProperty("id").GetString();

        using var response = await _client.GetAsync($"/v1/teams/{teamId}/form");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(teamId, json.RootElement.GetProperty("teamId").GetString());
        Assert.True(json.RootElement.GetProperty("played").GetInt32() >= 0);
        Assert.True(json.RootElement.TryGetProperty("form", out _));
    }

    [Fact]
    public async Task PlayerLoad_returns_an_aggregated_report()
    {
        // The finished matches are the ones seeded with player-load rows.
        var finished = await FirstMatchAsync("status=Finished&pageSize=100");
        var id = finished.GetProperty("id").GetString();

        using var response = await _client.GetAsync($"/v1/matches/{id}/players/load");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, json.RootElement.GetProperty("matchId").GetString());
        Assert.True(json.RootElement.GetProperty("playerCount").GetInt32() > 0);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private async Task<JsonElement> FirstMatchAsync(string query = "pageSize=1")
    {
        var payload = await _client.GetFromJsonAsync<JsonElement>($"/v1/matches?{query}");
        var items = payload.GetProperty("items");
        Assert.True(items.GetArrayLength() > 0, "expected at least one seeded match");
        return items[0].Clone();
    }
}
