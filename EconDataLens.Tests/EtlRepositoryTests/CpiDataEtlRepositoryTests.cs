using EconDataLens.Application.Cpi;
using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.EtlRepositoryTests;

public class CpiDataEtlRepositoryTests
{
    private EconDataLensDbContext _dbContext = null!;
    private CpiDataFileParser _parser = null!;
    private ICpiIngestionRepository _repository = null!;
    private string _connectionString = string.Empty;

    [SetUp]
    public async Task SetUp()
    {
        _connectionString = PostgresFixture.ConnectionString;
        var opts = new DbContextOptionsBuilder<EconDataLensDbContext>()
            .UseNpgsql(_connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await DbReset.RecreateDatabaseAsync(_connectionString);

        _dbContext = new EconDataLensDbContext(opts);

        _parser = new CpiDataFileParser();
        _repository = new CpiIngestionRepository(_dbContext);
    }

    [Test]
    public async Task UpsertCpiDataAsync_NewData_InsertsSuccessfully()
    {
        await UpsertDependencies();

        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.data.0.Current.sample");

        await _repository.UpsertCpiDataAsync(_parser.ParseCpiDataAsync(path));

        var count = await _dbContext.CpiData.CountAsync();

        Assert.That(count, Is.EqualTo(4));

        var sample = await _dbContext.CpiData.FirstOrDefaultAsync(d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.Value == 159.40m);
        });
    }

    [Test]
    public async Task UpsertCpiDataAsync_StreamFailsMidway_LeavesExistingRowsUnchanged()
    {
        await UpsertDependencies();
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.data.0.Current.sample");
        await _repository.UpsertCpiDataAsync(_parser.ParseCpiDataAsync(path));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.UpsertCpiDataAsync(FailingStream()));

        // The stream fails while rows are still being copied, before the upsert statement runs,
        // so the yielded M01 = 999.99 row never reaches cpi_data.
        var sample = await _dbContext.CpiData.SingleAsync(
            d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M01");
        Assert.That(sample.Value, Is.EqualTo(159.40m));
        Assert.That(await _dbContext.CpiData.CountAsync(), Is.EqualTo(4));
    }

    [Test]
    public async Task UpsertCpiDataAsync_UpsertViolatesForeignKey_RollsBackAndNextBatchSucceeds()
    {
        await UpsertDependencies();

        // The bad row only fails at the INSERT ... SELECT, after the COPY into the temp table
        // succeeded, so the connection stays alive and the transaction must be rolled back.
        Assert.CatchAsync(() => _repository.UpsertCpiDataAsync(Rows(
            new CpiData { SeriesId = "CUSR0000SA0", Year = 1997, Period = "M01", Value = 111.11m },
            new CpiData { SeriesId = "NOSUCHSERIES", Year = 1997, Period = "M01", Value = 1m })));

        Assert.That(await _dbContext.CpiData.CountAsync(), Is.EqualTo(0), "failed batch must not land");

        // A leftover tmp_cpi_data would make CREATE TEMP TABLE fail on the shared connection.
        await _repository.UpsertCpiDataAsync(Rows(
            new CpiData { SeriesId = "CUSR0000SA0", Year = 1997, Period = "M01", Value = 222.22m }));

        var row = await _dbContext.CpiData.SingleAsync();
        Assert.That(row.Value, Is.EqualTo(222.22m));
    }

    [Test]
    public async Task UpsertCpiDataAsync_UnpublishedValueLaterPublished_ReplacesValueAndClearsFootnote()
    {
        await UpsertDependencies();

        await _repository.UpsertCpiDataAsync(Rows(
            new CpiData { SeriesId = "CUSR0000SA0", Year = 1997, Period = "M01", Value = null, FootnoteCodes = "X" }));
        await _repository.UpsertCpiDataAsync(Rows(
            new CpiData { SeriesId = "CUSR0000SA0", Year = 1997, Period = "M01", Value = 160.0m, FootnoteCodes = null }));

        // The upsert overwrites with EXCLUDED.*, so a republished value must clear
        // the old footnote rather than keeping it via a "helpful" COALESCE.
        var row = await _dbContext.CpiData.SingleAsync(
            d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M01");
        Assert.Multiple(() =>
        {
            Assert.That(row.Value, Is.EqualTo(160.0m));
            Assert.That(row.FootnoteCodes, Is.Null);
        });
    }

    private static async IAsyncEnumerable<CpiData> FailingStream()
    {
        yield return new CpiData { SeriesId = "CUSR0000SA0", Year = 1997, Period = "M01", Value = 999.99m };
        await Task.Yield();
        throw new InvalidOperationException("Simulated mid-stream failure.");
    }

    private static async IAsyncEnumerable<CpiData> Rows(params CpiData[] rows)
    {
        foreach (var row in rows)
        {
            await Task.Yield();
            yield return row;
        }
    }

    [Test]
    public async Task UpsertCpiDataAsync_NullValue_StoresNullWithFootnote()
    {
        await UpsertDependencies();
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path,
            "series_id\tyear\tperiod\tvalue\tfootnote_codes\nCUSR0000SA0\t1997\tM01\t-\tX\n");
        try
        {
            await _repository.UpsertCpiDataAsync(_parser.ParseCpiDataAsync(path));
        }
        finally
        {
            File.Delete(path);
        }

        var row = await _dbContext.CpiData.SingleAsync(d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(row.Value, Is.Null);
            Assert.That(row.FootnoteCodes, Is.EqualTo("X"));
        });
    }

    [Test]
    public async Task UpsertCpiDataAsync_ExistingData_UpdatesSuccessfully()
    {
        await UpsertDependencies();
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.data.0.Current.sample");
        var modifiedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.data.0.Current.modified.sample");

        await _repository.UpsertCpiDataAsync(_parser.ParseCpiDataAsync(path));

        var count = await _dbContext.CpiData.CountAsync();

        Assert.That(count, Is.EqualTo(4));

        var sample = await _dbContext.CpiData.FirstOrDefaultAsync(d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.Value == 159.40m);
        });

        // Run the upsert again with modified data
        await _repository.UpsertCpiDataAsync(_parser.ParseCpiDataAsync(modifiedPath));

        count = await _dbContext.CpiData.CountAsync();

        Assert.That(count, Is.EqualTo(4));

        sample = await _dbContext.CpiData.FirstOrDefaultAsync(d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.Value == 999.99m);
        });

        // Verify another record was unchanged
        sample = await _dbContext.CpiData.FirstOrDefaultAsync(d => d.SeriesId == "CUSR0000SA0" && d.Year == 1997 && d.Period == "M02");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.Value, Is.EqualTo(159.70m));
        });

    }

    private async Task UpsertDependencies()
    {
        var areaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.area.sample");
        var itemPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.item.sample");
        var seriesPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.series.sample");
        var footnotePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.footnote.sample");
        var periodPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.period.sample");

        await _repository.UpsertCpiAreaAsync(_parser.ParseCpiAreasAsync(areaPath));
        await _repository.UpsertCpiPeriodAsync(_parser.ParseCpiPeriodsAsync(periodPath));
        await UpsertSeriesLookups();
        await _repository.UpsertCpiItemAsync(_parser.ParseCpiItemsAsync(itemPath));
        await _repository.UpsertCpiFootnotesAsync(_parser.ParseCpiFootnoteAsync(footnotePath));
        await _repository.UpsertCpiSeriesAsync(_parser.ParseCpiSeriesAsync(seriesPath));
    }


    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
    }

    private async Task UpsertSeriesLookups()
    {
        var dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData");
        await _repository.UpsertCpiPeriodicityAsync(_parser.ParseCpiPeriodicityAsync(Path.Combine(dir, "cu.periodicity.sample")));
        await _repository.UpsertCpiSeasonalAsync(_parser.ParseCpiSeasonalAsync(Path.Combine(dir, "cu.seasonal.sample")));
        await _repository.UpsertCpiBaseAsync(_parser.ParseCpiBaseAsync(Path.Combine(dir, "cu.base.sample")));
    }
}
