using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.EtlRepositoryTests;

public class AreaEtlRepositoryTests
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
    public async Task UpsertCpiAreaAsync_NewAreas_InsertsSuccessfully()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.area.sample");

        await _repository.UpsertCpiAreaAsync(_parser.ParseCpiAreasAsync(path));

        var count = await _dbContext.CpiArea.CountAsync();

        Assert.That(count, Is.EqualTo(58));

        var sample = await _dbContext.CpiArea.FirstOrDefaultAsync(a => a.AreaCode == "0000");

        Assert.That(sample!, Is.Not.Null);
        Assert.That(sample!.AreaName, Is.EqualTo("U.S. city average"));

        sample = await _dbContext.CpiArea.FirstOrDefaultAsync(a => a.AreaCode == "S49G");

        Assert.That(sample, Is.Not.Null);
        Assert.That(sample!.AreaName, Is.EqualTo("Urban Alaska"));

    }

    [Test]
    public async Task UpsertCpiAreaAsync_ExistingAreas_UpdatesSuccessfully()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.area.sample");

        await _repository.UpsertCpiAreaAsync(_parser.ParseCpiAreasAsync(path));

        var count = await _dbContext.CpiArea.CountAsync();

        Assert.That(count, Is.EqualTo(58));

        var sample = await _dbContext.CpiArea.FirstOrDefaultAsync(a => a.AreaCode == "0000");

        Assert.That(sample!, Is.Not.Null);
        Assert.That(sample!.AreaName, Is.EqualTo("U.S. city average"));

        sample = await _dbContext.CpiArea.FirstOrDefaultAsync(a => a.AreaCode == "S49G");

        Assert.That(sample, Is.Not.Null);
        Assert.That(sample!.AreaName, Is.EqualTo("Urban Alaska"));

        // Ingest modified data

        path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.area.modified.sample");

        await _repository.UpsertCpiAreaAsync(_parser.ParseCpiAreasAsync(path));

        count = await _dbContext.CpiArea.CountAsync();

        Assert.That(count, Is.EqualTo(58));

        sample = await _dbContext.CpiArea.FirstOrDefaultAsync(a => a.AreaCode == "S49G");

        Assert.That(sample!, Is.Not.Null);
        Assert.That(sample!.AreaName, Is.EqualTo("Urban Alaska UPDATED"));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
    }

}
