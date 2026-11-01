# Adding a dataset

Checklist for the next BLS survey (for example Average Price, `ap`), mirroring the CPI slice. Replace `<Survey>` with the survey name. Read [architecture.md](architecture.md) and [cpi-etl.md](cpi-etl.md) first.

1. **Domain**: entities in `EconDataLens.Domain/Entities/<Survey>/`, one per BLS file, with XML docs naming the source file and columns (see `CpiAspect`).
2. **Application** (`EconDataLens.Application/<Survey>/`):
   - `I<Survey>DataSource` returning `Task<IAsyncEnumerable<T>>` per file (download first, parse lazily).
   - `I<Survey>IngestionRepository` with one `Upsert*Async` per entity.
   - Ingestion and ETL services; the ETL service owns the run order (lookups, series, data).
   - Register them in `Application/DependencyInjection.cs`.
3. **Infrastructure**:
   - A `<Survey>Options` section under `BlsOptions` (base URL, file names); bind it in `Infrastructure/DependencyInjection.cs`.
   - A parser (stream lines, split on tab, trim; handle missing-value markers).
   - A data source reusing `BasicFileDownloadService`.
   - A repository using the temp table, binary `COPY`, `ON CONFLICT DO UPDATE` pattern.
   - `DbSet`s, keys and foreign keys in `EconDataLensDbContext`.
   - Register everything in `AddInfrastructure()`.
4. **Migration**: `dotnet ef migrations add Add<Survey>Tables --project EconDataLens.Infrastructure --startup-project EconDataLens.Etl --output-dir Persistence/Migrations`. Review the generated SQL; do not hand-edit.
5. **Etl**: call the new ETL service from `Program.cs`. Decide how exit codes behave when one survey fails and others succeed.
6. **Config**: add the section to `EconDataLens.Etl/appsettings.example.json`, any env vars to `docker-compose.yml`, and the README configuration table.
7. **Tests** (see [testing.md](testing.md)): small sample files under `TestData/`; parser tests; repository tests with `*.sample` and `*.modified.sample`; a use-case test for run order with fakes. Extend `DependencyRuleTests` only if you add a new assembly.

## Open design questions

Decide these in the PR rather than assuming:

- Whether a shared concept such as footnotes gets one table per survey or a shared table.
- Whether `Program.cs` runs every ETL on each invocation or selects one by argument.
