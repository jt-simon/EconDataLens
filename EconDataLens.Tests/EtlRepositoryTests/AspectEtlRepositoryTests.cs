using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.EtlRepositoryTests;

public class AspectEtlRepositoryTests
{
    private EconDataLensDbContext _dbContext = null!;
    private CpiDataFileParser _parser = null!;
    private ICpiIngestionRepository _repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        var connectionString = PostgresFixture.ConnectionString;
        var opts = new DbContextOptionsBuilder<EconDataLensDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await DbReset.RecreateDatabaseAsync(connectionString);

        _dbContext = new EconDataLensDbContext(opts);
        _parser = new CpiDataFileParser();
        _repository = new CpiIngestionRepository(_dbContext);

        await UpsertDependencies();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
    }

    private static string Etl(string file) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", file);

    private async Task UpsertDependencies()
    {
        await _repository.UpsertCpiAreaAsync(_parser.ParseCpiAreasAsync(Etl("cu.area.sample")));
        await _repository.UpsertCpiPeriodAsync(_parser.ParseCpiPeriodsAsync(Etl("cu.period.sample")));
        await UpsertSeriesLookups();
        await _repository.UpsertCpiItemAsync(_parser.ParseCpiItemsAsync(Etl("cu.item.sample")));
        await _repository.UpsertCpiFootnotesAsync(_parser.ParseCpiFootnoteAsync(Etl("cu.footnote.sample")));
        await _repository.UpsertCpiSeriesAsync(_parser.ParseCpiSeriesAsync(Etl("cu.series.sample")));
    }

    [Test]
    public async Task UpsertCpiAspectAsync_NewAspects_StoresTextAndNumericValues()
    {
        await _repository.UpsertCpiAspectAsync(_parser.ParseCpiAspectAsync(Etl("cu.aspect.sample")));

        var rows = await _dbContext.CpiAspect.AsNoTracking().OrderBy(a => a.AspectType).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].AspectType, Is.EqualTo("H1"));
            Assert.That(rows[0].Value, Is.EqualTo("S-Jan. 1996"));
            Assert.That(rows[1].AspectType, Is.EqualTo("M1"));
            Assert.That(rows[1].Value, Is.EqualTo("0.03"));
        });
    }

    [Test]
    public async Task UpsertCpiAspectAsync_ExistingAspect_UpdatesInPlace()
    {
        await _repository.UpsertCpiAspectAsync(_parser.ParseCpiAspectAsync(Etl("cu.aspect.sample")));
        await _repository.UpsertCpiAspectAsync(_parser.ParseCpiAspectAsync(Etl("cu.aspect.modified.sample")));

        Assert.That(await _dbContext.CpiAspect.CountAsync(), Is.EqualTo(2));
        var row = await _dbContext.CpiAspect.AsNoTracking().SingleAsync(a => a.AspectType == "M1");
        Assert.That(row.Value, Is.EqualTo("0.50"));
    }

    private async Task UpsertSeriesLookups()
    {
        var dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData");
        await _repository.UpsertCpiPeriodicityAsync(_parser.ParseCpiPeriodicityAsync(Path.Combine(dir, "cu.periodicity.sample")));
        await _repository.UpsertCpiSeasonalAsync(_parser.ParseCpiSeasonalAsync(Path.Combine(dir, "cu.seasonal.sample")));
        await _repository.UpsertCpiBaseAsync(_parser.ParseCpiBaseAsync(Path.Combine(dir, "cu.base.sample")));
    }
}
