<p align="center">
  <img src="icon.png" alt="GM.HealthChecks Samples" width="140" height="140" />
</p>

# GM.HealthChecks Samples

[![CI](https://github.com/gmetskhvarishvili/GM.HealthChecks.Samples/actions/workflows/ci.yml/badge.svg)](https://github.com/gmetskhvarishvili/GM.HealthChecks.Samples/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A minimal ASP.NET Core Web API that wires up **[GM.HealthChecks](https://www.nuget.org/packages/GM.HealthChecks)**:
`AddGMHealthChecks()` + provider checks + `MapGMHealthChecks()`, exposing `/health/live`,
`/health/ready` and `/health/startup` as consistent JSON. Targets **.NET 10**.

## What it demonstrates

- The one-call registration + endpoint mapping.
- Provider checks composed onto the builder: **database** (`AddGMDatabaseCheck<SampleDbContext>`),
  **cache** (`AddGMCacheCheck`), **distributed lock** (`AddGMDistributedLockCheck`), and an
  **external HTTP** dependency (`AddGMHttpCheck`).
- The **liveness/readiness split**: `/health/live` carries only the self-check (never external
  infra), while `/health/ready` carries the dependency checks.

It runs **green with no external services** — it uses SQLite plus the in-memory GM.Caching /
GM.DistributedLock backends. Swap in Npgsql, `GM.Caching.Redis`, `GM.DistributedLock.Redis`, and
`AddGMMessagingCheck()` (with `AddGMMessaging`) for a real deployment.

## Endpoints

| Route | What it checks |
| --- | --- |
| `/health/live` | Self-check only — is the process up? |
| `/health/ready` | Database + cache + distributed lock + external HTTP dependency |
| `/health/startup` | Same dependency set, for startup gating |

```bash
dotnet run --project GM.HealthChecks.Sample.API
curl -s http://localhost:5xxx/health/ready | jq
```

```json
{
  "status": "Healthy",
  "totalDurationMs": 9.7,
  "checks": [
    { "name": "gm:database", "status": "Healthy", "durationMs": 3.1, "tags": ["ready", "startup"] },
    { "name": "gm:cache",    "status": "Healthy", "durationMs": 0.4, "tags": ["ready", "startup"] },
    { "name": "gm:lock",     "status": "Healthy", "durationMs": 0.2, "tags": ["ready", "startup"] },
    { "name": "gm:http:example-dependency", "status": "Healthy", "durationMs": 5.9, "tags": ["ready", "startup"] }
  ]
}
```

## Testing

```bash
dotnet test
```

The tests boot the app in-memory with `WebApplicationFactory` and assert the JSON shape of
`/health/live` and `/health/ready` — no external infrastructure required.

## License

MIT — see [LICENSE](LICENSE).
