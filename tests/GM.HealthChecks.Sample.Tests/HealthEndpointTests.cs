using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GM.HealthChecks.Sample.Tests;

// Spins up the real sample app in-memory (SQLite + in-memory cache/lock) — no external infra needed.
public class HealthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Live_ReturnsHealthyJson()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType!.ToString());

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        // Liveness only carries the self-check — never external infra.
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray().ToList();
        Assert.Single(checks);
        Assert.Equal("gm:self", checks[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Ready_ReportsTheRegisteredDependencyChecks()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        // Readiness may be 200 or 503 depending on the external HTTP dependency, but it must always
        // return the GM JSON shape listing the dependency checks that were registered.
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = doc.RootElement.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("gm:database", names);
        Assert.Contains("gm:cache", names);
        Assert.Contains("gm:lock", names);
        Assert.DoesNotContain("gm:self", names); // self is liveness-only
    }
}
