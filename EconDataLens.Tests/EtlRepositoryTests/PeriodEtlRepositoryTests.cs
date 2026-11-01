using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.EtlRepositoryTests;

public class PeriodEtlRepositoryTests
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
    public async Task UpsertCpiPeriodsAsync_NewPeriods_InsertsSuccessfully()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.period.sample");

        await _repository.UpsertCpiPeriodAsync(_parser.ParseCpiPeriodsAsync(path));

        var count = await _dbContext.CpiPeriod.CountAsync();

        Assert.That(count, Is.EqualTo(16));

        var sample = await _dbContext.CpiPeriod.FirstOrDefaultAsync(p => p.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.PeriodName, Is.EqualTo("January"));
            Assert.That(sample!.PeriodAbbreviation, Is.EqualTo("JAN"));
        });
    }

    [Test]
    public async Task UpsertCpiPeriodsAsync_ExistingPeriods_UpdatesSuccessfully()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.period.sample");

        await _repository.UpsertCpiPeriodAsync(_parser.ParseCpiPeriodsAsync(path));

        var count = await _dbContext.CpiPeriod.CountAsync();

        Assert.That(count, Is.EqualTo(16));

        var sample = await _dbContext.CpiPeriod.FirstOrDefaultAsync(p => p.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.PeriodName, Is.EqualTo("January"));
            Assert.That(sample!.PeriodAbbreviation, Is.EqualTo("JAN"));
        });

        path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.period.modified.sample");
        await _repository.UpsertCpiPeriodAsync(_parser.ParseCpiPeriodsAsync(path));

        count = await _dbContext.CpiPeriod.CountAsync();

        Assert.That(count, Is.EqualTo(16));

        sample = await _dbContext.CpiPeriod.FirstOrDefaultAsync(p => p.Period == "M01");

        Assert.Multiple(() =>
        {
            Assert.That(sample, Is.Not.Null);
            Assert.That(sample!.PeriodName, Is.EqualTo("JanuaryM"));
            Assert.That(sample!.PeriodAbbreviation, Is.EqualTo("MOD"));
        });

    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
    }

}
