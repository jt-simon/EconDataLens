using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;

namespace EconDataLens.Tests.EtlParserTests;

[TestFixture]
public class CpiAspectParserTests
{
    private readonly CpiDataFileParser _parser = new();

    private static string ParserFile(string file) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", file);

    private static async Task<List<CpiAspect>> ToListAsync(IAsyncEnumerable<CpiAspect> source)
    {
        var rows = new List<CpiAspect>();
        await foreach (var row in source) rows.Add(row);
        return rows;
    }

    [Test]
    public async Task ParseCpiAspectAsync_HeaderOnly_YieldsNoResults()
    {
        var rows = await ToListAsync(_parser.ParseCpiAspectAsync(ParserFile("cu.aspect.empty")));

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiAspectAsync_FileWithRecords_KeepsTextAndNumericValuesTrimmed()
    {
        var rows = await ToListAsync(_parser.ParseCpiAspectAsync(ParserFile("cu.aspect.sample")));

        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[0].SeriesId, Is.EqualTo("CUUR0000SA0"));
            Assert.That(rows[0].Year, Is.EqualTo(2012));
            Assert.That(rows[0].Period, Is.EqualTo("M03"));
            Assert.That(rows[0].AspectType, Is.EqualTo("HC"));
            Assert.That(rows[0].Value, Is.EqualTo("S-Mar. 2011"));
            Assert.That(rows[0].FootnoteCodes, Is.Null);
            Assert.That(rows[1].AspectType, Is.EqualTo("MC"));
            Assert.That(rows[1].Value, Is.EqualTo("0.07"));
            Assert.That(rows[2].SeriesId, Is.EqualTo("CUSR0000SAA1"));
            Assert.That(rows[2].AspectType, Is.EqualTo("F"));
            Assert.That(rows[2].Value, Is.EqualTo("100.976"));
        });
    }

    [Test]
    public async Task ParseCpiAspectAsync_RowWithoutFootnoteColumn_YieldsNullFootnote()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path,
            "series_id\tyear\tperiod\taspect_type\tvalue\nCUSR0000SA0\t2012\tM03\tM1\t0.03\n");
        try
        {
            var rows = await ToListAsync(_parser.ParseCpiAspectAsync(path));

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].FootnoteCodes, Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ParseCpiAspectAsync_TooFewColumns_ThrowsFormatException()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "series_id\tyear\tperiod\taspect_type\tvalue\nCUSR0000SA0\t2012\tM03\n");
        try
        {
            Assert.ThrowsAsync<FormatException>(async () => await ToListAsync(_parser.ParseCpiAspectAsync(path)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
