# AGENTS.md

Instructions for AI coding agents (and a quick orientation for humans). Keep this file short;
depth lives in [`docs/`](docs/).

## What this is

A .NET 10 console ETL that downloads U.S. Bureau of Labor Statistics (BLS) Consumer Price Index
flat files (`https://download.bls.gov/pub/time.series/cu/`) and loads them into PostgreSQL.
Only the CPI pipeline exists today. There is no web UI yet.

## Repo map

| Project | Role |
| --- | --- |
| `EconDataLens.Domain` | Plain entities (`Entities/Cpi/*`). No project or package references. |
| `EconDataLens.Application` | Use cases `CpiEtlService` (run order) and `CpiIngestionService`; ports `ICpiDataSource`, `ICpiIngestionRepository`; `AddApplication()`. |
| `EconDataLens.Infrastructure` | `Bls/` (download, `CpiDataFileParser`, `BlsCpiDataSource`, options) and `Persistence/` (`EconDataLensDbContext`, `CpiIngestionRepository`, `Migrations/`); `AddInfrastructure()`. |
| `EconDataLens.Etl` | The only composition root (`Program.cs`). Exit codes: `0` ok, `1` ETL failed, `2` DB connect/migrate failed. |
| `EconDataLens.Tests` | NUnit, grouped by layer. See [docs/testing.md](docs/testing.md). |

## Commands

```bash
dotnet restore && dotnet tool restore
dotnet build
dotnet test EconDataLens.Tests/EconDataLens.Tests.csproj                      # needs Docker
dotnet test EconDataLens.Tests/EconDataLens.Tests.csproj --filter "FullyQualifiedName~CpiAreaParserTests"
dotnet ef migrations add <Name> --project EconDataLens.Infrastructure --startup-project EconDataLens.Etl --output-dir Persistence/Migrations
cd EconDataLens.Etl && dotnet run                                              # needs appsettings.json or env vars
docker compose up --build etl                                                  # needs .env (see README)
```

Docker must be running for any test run: `PostgresFixture` starts a Testcontainers Postgres for the whole assembly, even for tests that do not use the database.

## Rules

- Dependency rule: Domain ← Application ← Infrastructure ← Etl. `Tests/Architecture/DependencyRuleTests.cs` enforces it.
- New I/O (HTTP, DB, filesystem) goes behind a port in Application and is implemented in Infrastructure. Wire it in `Etl` via the `Add*` extension methods.
- Warnings are errors (`Directory.Build.props`).
- Public types get XML doc comments (see `CONTRIBUTING.md`).
- Generate migrations with `dotnet ef`; do not hand-edit them. Commit the updated model snapshot.
- Never commit `.env`, `appsettings.json`, or `downloads/` (all gitignored).
- Branch names: `fix/…`, `feature/…`, `chore/…`. Open an issue first for larger changes.
- Tests: fakes over mocks, one behavior per test, named `Method_Condition_Expected`.

## Gotchas

- `appsettings.json` and the `downloads` directory resolve from the current working directory, so run from `EconDataLens.Etl/` or use env vars (`__` for nesting).
- BLS returns 403 for a missing/generic User-Agent **and** for datacenter/VPN IPs. In a cloud sandbox the second is likely; do not change code to "fix" it. See README → Troubleshooting.
- `cpi_data.value` is nullable: BLS writes `-` when no value was published.
- Import order matters (foreign keys; historical partitions before `cu.data.0.Current`). See [docs/cpi-etl.md](docs/cpi-etl.md).
- Bulk upserts use raw Npgsql `COPY`, so the connection string's `Command Timeout` matters (raised to 600s in `Infrastructure/DependencyInjection.cs`).
- Table and column names are snake_case (`UseSnakeCaseNamingConvention`).

## Read next

- [docs/architecture.md](docs/architecture.md): layers, ports and adapters, composition root, configuration flow.
- [docs/cpi-etl.md](docs/cpi-etl.md): run order, file-to-table map, persistence pattern, data quirks.
- [docs/testing.md](docs/testing.md): fixtures, folders, test data, running tests.
- [docs/adding-a-dataset.md](docs/adding-a-dataset.md): checklist for the next BLS survey.
- [README.md](README.md): Docker usage, configuration table, troubleshooting, historical backfill.
