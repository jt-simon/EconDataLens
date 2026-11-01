using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.EtlRepositoryTests;

public class LookupEtlRepositoryTests
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
    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
    }

    private static string Etl(string file) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", file);

    [Test]
    public async Task UpsertCpiPeriodicityAsync_ExistingRow_UpdatesInPlace()
    {
        await _repository.UpsertCpiPeriodicityAsync(_parser.ParseCpiPeriodicityAsync(Etl("cu.periodicity.sample")));
        Assert.That(await _dbContext.CpiPeriodicity.CountAsync(), Is.EqualTo(2));

        await _repository.UpsertCpiPeriodicityAsync(
            _parser.ParseCpiPeriodicityAsync(Etl("cu.periodicity.modified.sample")));

        Assert.That(await _dbContext.CpiPeriodicity.CountAsync(), Is.EqualTo(2));
        var row = await _dbContext.CpiPeriodicity.AsNoTracking().SingleAsync(x => x.PeriodicityCode == "R");
        Assert.That(row.PeriodicityName, Is.EqualTo("modified"));
    }

    [Test]
    public async Task UpsertCpiSeasonalAsync_ExistingRow_UpdatesInPlace()
    {
        await _repository.UpsertCpiSeasonalAsync(_parser.ParseCpiSeasonalAsync(Etl("cu.seasonal.sample")));
        Assert.That(await _dbContext.CpiSeasonal.CountAsync(), Is.EqualTo(2));

        await _repository.UpsertCpiSeasonalAsync(_parser.ParseCpiSeasonalAsync(Etl("cu.seasonal.modified.sample")));

        Assert.That(await _dbContext.CpiSeasonal.CountAsync(), Is.EqualTo(2));
        var row = await _dbContext.CpiSeasonal.AsNoTracking().SingleAsync(x => x.SeasonalCode == "S");
        Assert.That(row.SeasonalText, Is.EqualTo("modified"));
    }

    [Test]
    public async Task UpsertCpiBaseAsync_ExistingRow_UpdatesInPlace()
    {
        await _repository.UpsertCpiBaseAsync(_parser.ParseCpiBaseAsync(Etl("cu.base.sample")));
        Assert.That(await _dbContext.CpiBase.CountAsync(), Is.EqualTo(2));

        await _repository.UpsertCpiBaseAsync(_parser.ParseCpiBaseAsync(Etl("cu.base.modified.sample")));

        Assert.That(await _dbContext.CpiBase.CountAsync(), Is.EqualTo(2));
        var row = await _dbContext.CpiBase.AsNoTracking().SingleAsync(x => x.BaseCode == "A");
        Assert.That(row.BaseName, Is.EqualTo("modified"));
    }
}
