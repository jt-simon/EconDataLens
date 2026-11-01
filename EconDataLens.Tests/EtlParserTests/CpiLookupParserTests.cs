using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;

namespace EconDataLens.Tests.EtlParserTests;

[TestFixture]
public class CpiLookupParserTests
{
    private readonly CpiDataFileParser _parser = new();

    private static string Path_(string file) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", file);

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var rows = new List<T>();
        await foreach (var row in source) rows.Add(row);
        return rows;
    }

    [Test]
    public async Task ParseCpiPeriodicityAsync_FileWithRecords_YieldsTrimmedResults()
    {
        var rows = await ToListAsync(_parser.ParseCpiPeriodicityAsync(Path_("cu.periodicity.sample")));

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].PeriodicityCode, Is.EqualTo("R"));
            Assert.That(rows[0].PeriodicityName, Is.EqualTo("Monthly"));
            Assert.That(rows[1].PeriodicityCode, Is.EqualTo("S"));
            Assert.That(rows[1].PeriodicityName, Is.EqualTo("Semi-Annual"));
        });
    }

    [Test]
    public async Task ParseCpiSeasonalAsync_FileWithRecords_YieldsResults()
    {
        var rows = await ToListAsync(_parser.ParseCpiSeasonalAsync(Path_("cu.seasonal.sample")));

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].SeasonalCode, Is.EqualTo("S"));
            Assert.That(rows[0].SeasonalText, Is.EqualTo("Seasonally Adjusted"));
            Assert.That(rows[1].SeasonalCode, Is.EqualTo("U"));
        });
    }

    [Test]
    public async Task ParseCpiBaseAsync_FileWithRecords_YieldsResults()
    {
        var rows = await ToListAsync(_parser.ParseCpiBaseAsync(Path_("cu.base.sample")));

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].BaseCode, Is.EqualTo("A"));
            Assert.That(rows[0].BaseName, Is.EqualTo("Alternate"));
            Assert.That(rows[1].BaseCode, Is.EqualTo("S"));
        });
    }

    [Test]
    public async Task ParseCpiBaseAsync_HeaderOnly_YieldsNoResults()
    {
        var rows = await ToListAsync(_parser.ParseCpiBaseAsync(Path_("cu.base.empty")));

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void ParseCpiBaseAsync_MissingFile_Throws()
    {
        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await ToListAsync(_parser.ParseCpiBaseAsync(Path_("does.not.exist"))));
    }

    [Test]
    public void ParseCpiSeasonalAsync_RowWithOneColumn_ThrowsFormatException()
    {
        var path = System.IO.Path.GetTempFileName();
        File.WriteAllText(path, "seasonal_code\tseasonal_text\nS\n");
        try
        {
            Assert.ThrowsAsync<FormatException>(async () =>
                await ToListAsync(_parser.ParseCpiSeasonalAsync(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
