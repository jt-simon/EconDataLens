# CPI ETL

How the CPI pipeline works, so you do not have to read `CpiEtlService`, the parser, and the repository to find out.

## Run order

`CpiEtlService.RunEtlAsync` runs in phases because of foreign keys:

1. **Lookups**: area, footnote, item, period, periodicity, seasonal, base.
2. **Series** (references area, item, periodicity, seasonal, base, period).
3. **Data**: historical partitions first (one at a time), then `cu.data.0.Current` last, so the current file, which carries revisions, wins.
4. **Aspects** (references series and period).

## Files to tables

BLS files are tab-delimited with a header row; the parser trims every field and streams line by line.

| BLS file | Entity | Table | Primary key |
| --- | --- | --- | --- |
| `cu.area` | `CpiArea` | `cpi_area` | `area_code` |
| `cu.footnote` | `CpiFootnote` | `cpi_footnote` | `footnote_code` |
| `cu.item` | `CpiItem` | `cpi_item` | `item_code` |
| `cu.period` | `CpiPeriod` | `cpi_period` | `period` |
| `cu.periodicity` | `CpiPeriodicity` | `cpi_periodicity` | `periodicity_code` |
| `cu.seasonal` | `CpiSeasonal` | `cpi_seasonal` | `seasonal_code` |
| `cu.base` | `CpiBase` | `cpi_base` | `base_code` |
| `cu.series` | `CpiSeries` | `cpi_series` | `series_id` |
| `cu.data.*` | `CpiData` | `cpi_data` | `series_id, year, period` |
| `cu.aspect` | `CpiAspect` | `cpi_aspect` | `series_id, year, period, aspect_type` |

Keys and foreign keys are defined in `Infrastructure/Persistence/EconDataLensDbContext.cs`. File names and the historical partition list come from `BlsOptions:CPI` (see the README's [historical backfill](../README.md#historical-backfill)).

## Persistence pattern

Every `CpiIngestionRepository.Upsert*Async` does the same thing inside one transaction:

1. `CREATE TEMP TABLE tmp_<table>` matching the target.
2. Stream rows into it with a single binary `COPY ... FROM STDIN (FORMAT BINARY)`.
3. `INSERT ... SELECT ... ON CONFLICT (<key>) DO UPDATE` into the real table.

This exists because `cu.data.0.Current` has over a million rows; EF change tracking would be far too slow. Raw `NpgsqlCommand`s take their timeout from the connection string, hence the 600s floor in `Infrastructure/DependencyInjection.cs`. New tables should follow the same pattern.

## Download cache

`BasicFileDownloadService` saves files under `DownloadOptions:DownloadDirectory`. `DeleteDownloadedFiles` removes them after processing; `SkipDownload` parses what is already on disk and fails naming any missing file. Details in the README's [configuration](../README.md#configuration) section.

## Data quirks

- BLS writes `-` in the value column when no value was published (for example, months affected by the 2025 lapse in appropriations, footnote `X`). `CpiData.Value` is `decimal?`; the row is kept with its footnote code.
- `CpiAspect.Value` is text because it mixes numbers (`0.03`) with markers (`S-Jan. 2012`). Aspect types have no BLS lookup file.
- Footnote codes are stored as a raw string (`FootnoteCodes`), not a join table.
