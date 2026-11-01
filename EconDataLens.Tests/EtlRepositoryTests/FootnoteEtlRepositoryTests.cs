using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.EtlRepositoryTests;

public class FootnoteEtlRepositoryTests
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
    public async Task UpsertCpiFootnoteAsync_NewFootnote_InsertsSuccessfully()
    {
        var footnotePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.footnote.sample");

        await _repository.UpsertCpiFootnotesAsync(_parser.ParseCpiFootnoteAsync(footnotePath));

        var count = await _dbContext.CpiFootnote.CountAsync();

        Assert.That(count, Is.EqualTo(2));

        var sample = await _dbContext.CpiFootnote.FirstOrDefaultAsync(f => f.FootnoteCode == "1");

        Assert.That(sample!.FootnoteText, Is.EqualTo("This is a footnote"));
    }

    [Test]
    public async Task UpsertFootnoteAsync_ExistingFootnote_UpdatesSuccessfully()
    {
        var footnotePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.footnote.sample");
        var footnoteModifiedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData", "cu.footnote.modified.sample");

        await _repository.UpsertCpiFootnotesAsync(_parser.ParseCpiFootnoteAsync(footnotePath));

        var count = await _dbContext.CpiFootnote.CountAsync();

        Assert.That(count, Is.EqualTo(2));

        // Re-ingest with modified data
        await _repository.UpsertCpiFootnotesAsync(_parser.ParseCpiFootnoteAsync(footnoteModifiedPath));
        count = await _dbContext.CpiFootnote.CountAsync();

        Assert.That(count, Is.EqualTo(2));

        var sample = await _dbContext.CpiFootnote.FirstOrDefaultAsync(f => f.FootnoteCode == "1");

        Assert.That(sample!.FootnoteText, Is.EqualTo("modified"));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
    }
}
