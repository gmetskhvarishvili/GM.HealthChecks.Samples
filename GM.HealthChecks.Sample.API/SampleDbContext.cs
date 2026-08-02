using Microsoft.EntityFrameworkCore;

namespace GM.HealthChecks.Sample.API;

/// <summary>A tiny DbContext so the database health check has something real to connect to.</summary>
public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<Widget> Widgets => Set<Widget>();
}

public sealed class Widget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
