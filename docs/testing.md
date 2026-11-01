# Testing

NUnit 4 in `EconDataLens.Tests`.

```bash
dotnet test EconDataLens.Tests/EconDataLens.Tests.csproj
dotnet test EconDataLens.Tests/EconDataLens.Tests.csproj --filter "FullyQualifiedName~CpiAreaParserTests"
dotnet test EconDataLens.Tests/EconDataLens.Tests.csproj --filter "Name~UpsertCpiAreaAsync_NewAreas_InsertsSuccessfully"
```

## Docker requirement

`PostgresFixture` is a `[SetUpFixture]` that starts one `postgres:18-alpine` container (Testcontainers) for the whole run and applies migrations. It exposes `PostgresFixture.ConnectionString`. Repository tests call `DbReset.RecreateDatabaseAsync` in `SetUp` to drop and re-migrate the database, so each test starts clean. The fixture runs for every test run, so Docker must be available even for tests that do not use the database.

## Layout

| Folder | What it covers | Uses DB |
| --- | --- | --- |
| `EtlParserTests/` | `CpiDataFileParser` against `TestData/ParserData/*.sample` and `*.empty` | no |
| `EtlRepositoryTests/` | `CpiIngestionRepository` upserts on real Postgres. `TestData/EtlData/*.sample` loads first, then `*.modified.sample` proves existing rows update in place | yes |
| `EtlDataSourceTests/` | `BlsCpiDataSource` and `BasicFileDownloadService`; in-file stub `HttpMessageHandler`s, temp dirs | no |
| `Application/` | `CpiEtlService` / `CpiIngestionService` with in-file fakes (`FakeSource`, `RecordingRepository`); asserts run order | no |
| `Architecture/` | Dependency rule via referenced assemblies | no |
| `CompositionRoot/` | `Program.RunAsync` exit codes (0/1/2), run on `Program.CreateHostBuilder` with the default configuration sources replaced by an in-memory collection so the environment and `appsettings.json` cannot leak in; and DI wiring behavior (e.g. command timeout). Runs against the fixture DB; `Tests` references `Etl` for this (the architecture tests only restrict Domain and Application, so the rule still holds) | yes |
| `Persistence/` | EF Core model matches the latest migration (`HasPendingModelChanges`) | no |

## Conventions

- Fakes over mocks; assert call order only where order is the behavior (see `CpiEtlServiceTests`).
- One behavior per test, named `Method_Condition_Expected`.
- Test data lives under `TestData/` and is copied to the output directory (`Content Include="TestData/**"` in the csproj). Locate files with `Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", ...)`.
- Keep sample files small; they are excerpts of the real BLS files.
- `TestData/ParserData/*.sample` rows are verbatim excerpts of the real BLS files, CRLF line endings and column padding included (`.gitattributes` marks `TestData/**` as `-text` so git never rewrites them). Rows in `cu.data.0.Current.sample` each cover a quirk: `M13` and `S01` periods, 1- and 3-decimal values, footnote `R`, and the `-` / `X` unpublished value.
- To refresh a sample, copy rows from a local download in `EconDataLens.Etl/downloads/`. Never fetch from BLS in tests or CI (BLS returns 403 for datacenter IPs).
- For a bug fix, write the failing test first.
