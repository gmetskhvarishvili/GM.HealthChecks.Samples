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
    .AddGMHttpCheck("example-dependency", new Uri("https://example.com"));
// A RabbitMQ check would be: .AddGMMessagingCheck() after builder.Services.AddGMMessaging(config).

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// /health/live (self only), /health/ready (all deps), /health/startup — all JSON.
app.MapGMHealthChecks();

app.MapGet("/", () => Results.Ok(new
{
    message = "GM.HealthChecks sample",
    endpoints = new[] { "/health/live", "/health/ready", "/health/startup" },
}));

app.Run();

// Exposed so the test project can spin the app up with WebApplicationFactory.
public partial class Program;
