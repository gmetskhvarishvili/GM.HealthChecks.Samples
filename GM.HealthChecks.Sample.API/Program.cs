using GM.Caching;
using GM.DistributedLock;
using GM.HealthChecks;
using GM.HealthChecks.Caching;
using GM.HealthChecks.DistributedLock;
using GM.HealthChecks.EntityFramework;
using GM.HealthChecks.Sample.API;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Infra used by the checks. SQLite + in-memory cache/lock keep the sample green with no external
// services — swap in Npgsql / GM.Caching.Redis / GM.DistributedLock.Redis for the real thing.
builder.Services.AddDbContext<SampleDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Database") ?? "Data Source=healthchecks-sample.db"));
builder.Services.AddGMCaching();
builder.Services.AddGMDistributedLock();

// One builder, add only the checks this service needs. Tags default to readiness + startup.
builder.Services.AddGMHealthChecks()
    .AddGMDatabaseCheck<SampleDbContext>()
    .AddGMCacheCheck()
    .AddGMDistributedLockCheck()
#pragma warning disable S1075 // A fixed, well-known placeholder URL is the point of this sample check.
    .AddGMHttpCheck("example-dependency", new Uri("https://example.com"));
#pragma warning restore S1075
// A RabbitMQ check would be: .AddGMMessagingCheck() after builder.Services.AddGMMessaging(config).

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// /health/live (self only), /health/ready (all deps), /health/startup — all JSON.
app.MapGMHealthChecks();

// Hoisted so the minimal API delegate below doesn't allocate a new array on every request.
string[] sampleEndpoints = ["/health/live", "/health/ready", "/health/startup"];

app.MapGet("/", () => Results.Ok(new
{
    message = "GM.HealthChecks sample",
    endpoints = sampleEndpoints,
}));

await app.RunAsync();

// Exposed so the test project can spin the app up with WebApplicationFactory.
public partial class Program
{
    protected Program()
    {
    }
}
