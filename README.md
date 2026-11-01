# Econ Data Lens

**Econ Data Lens** is an open-source project with the goal of making public economic data more accessible, understandable, and useful.

The platform will eventually provide a **web interface** where users can browse and visualize datasets such as:
- Consumer Price Index (CPI)
- Average Price Index
- Employment and labor market data
- Additional Bureau of Labor Statistics (BLS) and other public datasets

The data will be transformed into a clean, digestible format to help researchers, educators, journalists, and the public gain insight into the economy.

---

## Project Status

✅ The **ETL pipeline for CPI data** is complete.  
🚧 Additional ETL pipelines (e.g., Average Price Index, employment datasets) are planned.  
🌐 Web front-end for browsing and visualization is in the planning stage.

---

## For Developers

### Requirements
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download) (local development only)
- [Docker](https://www.docker.com/) with Compose v2

### Run the ETL with Docker

The fastest way to run the full ETL locally. This starts PostgreSQL and the ETL container; the ETL applies migrations itself, downloads the BLS CPI files, and ingests them.

First create your settings file (`.env` is gitignored and read by Compose):

```bash
cp .env.example .env
# edit .env: set POSTGRES_PASSWORD and BLS_CONTACT_EMAIL
```

Then start it:

```bash
docker compose up --build etl
```

Compose refuses to start if `POSTGRES_PASSWORD` or `BLS_CONTACT_EMAIL` is missing. Postgres is published on `127.0.0.1` only (port `POSTGRES_PORT`, default `5432`), so it is reachable from your machine but not from the network.

Follow progress with `docker compose logs -f etl`; the ETL container exits when the run completes (exit code `0` on success, non-zero on failure — use `docker compose ps` to see the status). The PostgreSQL data and the BLS download cache persist in named volumes, so subsequent runs only fetch what changed.

Useful commands:

```bash
# Run again later
docker compose run --rm etl

# Query the database directly
docker compose exec postgres psql -U econdatalens -d econdatalens

# Tear everything down, including the database
docker compose down            # keep data
docker compose down -v         # delete the database volume (full clean-slate reload on next run)
```

`BLS_CONTACT_EMAIL` becomes the ETL's `User-Agent` (`EconDataLens/1.0 (<your email>)`). BLS blocks anonymous and browser-spoofed clients, so use an address you control.

The Postgres password is only applied when the database volume is first created. If you change `POSTGRES_PASSWORD` later, either run `docker compose down -v` (deletes the data) or change it in the database with `ALTER USER`.

**Deploying.** `.env` is not in the repository, so supply the same variables on the target host: create a `.env` next to `docker-compose.yml`, or set them through your platform's environment/secrets mechanism. The image runs as a non-root user and contains no credentials; the ETL reads everything from environment variables.

To backfill the full CPI history (back to 1913), set the 20 partition files via environment override before starting the ETL, e.g. in a `docker-compose.override.yml`:

```yaml
services:
  etl:
    environment:
      BlsOptions__CPI__HistoricalDataFiles__0: cu.data.1.AllItems
      BlsOptions__CPI__HistoricalDataFiles__1: cu.data.2.Summaries
      # ... continue through BlsOptions__CPI__HistoricalDataFiles__19: cu.data.20.USCommoditiesServicesSpecial
```

(The commented lines in `docker-compose.yml` list all 20 files.)

See the historical backfill section below for the full file list.

### Documentation

Developer and agent docs live in [`docs/`](docs/) (architecture, CPI ETL, testing, adding a dataset); [`AGENTS.md`](AGENTS.md) is the short entry point.

### Local development setup

Clone the repo and restore dependencies:

```bash
git clone https://github.com/jtsimon93/econ-data-lens.git
cd econ-data-lens
dotnet restore
dotnet tool restore
```

Apply EF Core migrations and update the database:

```bash
dotnet ef database update --project EconDataLens.Infrastructure --startup-project EconDataLens.Etl
```

Run the ETL (CPI ingestion):

```bash
dotnet run --project EconDataLens.Etl
```

The program exits with a status code: `0` on success, `1` if the ETL fails, `2` if the database connection/migration fails.

### Architecture

- `EconDataLens.Domain` — entities only, no project or package references.
- `EconDataLens.Application` — use cases (`CpiIngestionService`, `CpiEtlService`) and ports (`ICpiDataSource`, `ICpiIngestionRepository`); depends only on Domain.
- `EconDataLens.Infrastructure` — EF Core persistence, BLS download/parse adapters; implements Application ports.
- `EconDataLens.Etl` — the only composition root; wires `AddApplication()` + `AddInfrastructure()` and runs the ETL.

Dependency rule: Domain has no dependencies, Application depends only on Domain, Infrastructure implements Application ports, Etl wires everything.

### Configuration

Settings are read from `appsettings.json` and environment variables (environment variables win; use `__` for nesting, e.g. `BlsOptions__UserAgent`). Copy `EconDataLens.Etl/appsettings.example.json` to `EconDataLens.Etl/appsettings.json` (gitignored) for a starter file.

> **Run from `EconDataLens.Etl/`.** `appsettings.json` and the relative `downloads` directory are resolved from the current working directory, so `dotnet run --project EconDataLens.Etl` from the repo root finds no settings. Use `cd EconDataLens.Etl && dotnet run`, or set environment variables.

| Setting | Default | Description |
| --- | --- | --- |
| `ConnectionStrings:Postgres` | — (required) | Postgres connection string. For the Compose database: `Host=localhost;Port=5432;Database=econdatalens;Username=econdatalens;Password=<POSTGRES_PASSWORD from your .env>`. |
| `BlsOptions:UserAgent` | — (required) | `User-Agent` sent to BLS, e.g. `EconDataLens/1.0 (you@example.com)`. BLS blocks anonymous and browser-spoofed agents, so identify the app and include a contact address you control. Required unless `DownloadOptions:SkipDownload` is `true`; if it is empty the ETL exits with code `1` before touching the database, naming this setting. With Docker Compose, set `BLS_CONTACT_EMAIL` in a `.env` file instead. |
| `BlsOptions:CPI:*` | see example file | BLS base URL and file names, plus `HistoricalDataFiles` (see [Historical backfill](#historical-backfill)). |
| `DownloadOptions:DownloadDirectory` | `downloads` | Where BLS files are saved, relative to the working directory. |
| `DownloadOptions:DeleteDownloadedFiles` | `false` | Delete downloaded files after processing. |
| `DownloadOptions:SkipDownload` | `false` | Never contact BLS; parse the files already in `DownloadDirectory`. A missing file fails the run with an error naming it, so download everything once (leave this `false`) before turning it on. |

**Missing CPI values.** BLS publishes `-` instead of a number when no value exists (for example the months affected by the 2025 lapse in appropriations, footnote `X`). These rows are stored with a `NULL` `value` and keep their footnote code, so query with that in mind (e.g. `WHERE value IS NOT NULL`).

**Database command timeout.** The bulk upserts can run for a while (`cu.data.0.Current` is over a million rows). The ETL raises Npgsql's `Command Timeout` from the 30 second default to 600 seconds. If your connection string sets `Command Timeout=0` (no limit) or a value of 600 or more, your value is used.

### Troubleshooting

**BLS downloads return 403 (Access Denied).** There are two separate causes:

1. *The User-Agent.* BLS rejects generic and browser-spoofed agents. Make sure
   `BlsOptions:UserAgent` identifies the app and includes a contact address
   (`EconDataLens/1.0 (you@example.com)`). Check this first.
2. *The network.* BLS's CDN (Akamai) also blocks datacenter, VPN, and some other IP
   ranges regardless of headers — curl, a browser, and the app all get 403s from
   those networks (the `api.bls.gov` API is a different endpoint and may still
   respond). If you see this, run from a residential/unblocked network, or contact
   BLS via the error page's contact form.

### Historical backfill

By default the ETL loads `cu.data.0.Current`, which only covers 1997 onward. BLS publishes earlier
history (back to 1913) in 20 partitioned files. To load them once, list them in
`BlsOptions:CPI:HistoricalDataFiles`:

```json
"HistoricalDataFiles": [
  "cu.data.1.AllItems", "cu.data.2.Summaries", "cu.data.3.AsizeNorthEast",
  "cu.data.4.AsizeNorthCentral", "cu.data.5.AsizeSouth", "cu.data.6.AsizeWest",
  "cu.data.7.OtherNorthEast", "cu.data.8.OtherNorthCentral", "cu.data.9.OtherSouth",
  "cu.data.10.OtherWest", "cu.data.11.USFoodBeverage", "cu.data.12.USHousing",
  "cu.data.13.USApparel", "cu.data.14.USTransportation", "cu.data.15.USMedical",
  "cu.data.16.USRecreation", "cu.data.17.USEducationAndCommunication",
  "cu.data.18.USOtherGoodsAndServices", "cu.data.19.PopulationSize",
  "cu.data.20.USCommoditiesServicesSpecial"
]
```

Leave the list empty (the default) for regular runs. The partitions overlap, so they are loaded
one at a time and the current file is always applied last.

---

## Contributing

Contributions are welcome! Please see the [CONTRIBUTING.md](CONTRIBUTING.md) file for guidelines.

General process:
1. Open an [issue](../../issues) to propose significant changes or new features.
2. Fork the repository and create a feature branch.
3. Submit a pull request with a clear description of your changes.


---

## License

Copyright (c) 2025 Justin Simon

This project is licensed under the terms of the **GNU Affero General Public License v3.0 (AGPL-3.0)**.  
See the [LICENSE](LICENSE) file for details.
