using System.Globalization;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Domain.Entities.Cpi;


namespace EconDataLens.Tests.EtlParserTests;

public class CpiDataParserTests
{
    private CpiDataFileParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CpiDataFileParser();
    }

    [Test]
    public async Task ParseCpiDataAsync_HeaderOnly_YieldsNoResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.data.0.Current.empty");
        var rows = new List<CpiData>();

        await foreach (var row in _parser.ParseCpiDataAsync(path))
            rows.Add(row);

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiDataAsync_FileWithRecords_YieldsResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.data.0.Current.sample");
        var rows = new List<CpiData>();

        await foreach (var row in _parser.ParseCpiDataAsync(path))
            rows.Add(row);

        Assert.That(rows, Has.Count.EqualTo(5));

        Assert.Multiple(() =>
        {
            Assert.That(rows[0].SeriesId, Is.EqualTo("CUUR0000AA0"));
            Assert.That(rows[0].Year, Is.EqualTo(1997));
            Assert.That(rows[0].Period, Is.EqualTo("M13"));
            Assert.That(rows[0].Value, Is.EqualTo(480.8m));
            Assert.That(rows[0].FootnoteCodes, Is.Null);

            Assert.That(rows[1].Period, Is.EqualTo("S01"));
            Assert.That(rows[1].Value, Is.EqualTo(479.0m));

            Assert.That(rows[2].SeriesId, Is.EqualTo("CUSR0000SA0"));
            Assert.That(rows[2].Period, Is.EqualTo("M01"));
            Assert.That(rows[2].Value, Is.EqualTo(203.437m));

            Assert.That(rows[3].Period, Is.EqualTo("M10"));
            Assert.That(rows[3].Value, Is.Null);
            Assert.That(rows[3].FootnoteCodes, Is.EqualTo("X"));

            Assert.That(rows[4].SeriesId, Is.EqualTo("CUUSS35DSA0E"));
            Assert.That(rows[4].Year, Is.EqualTo(2026));
            Assert.That(rows[4].Period, Is.EqualTo("S01"));
            Assert.That(rows[4].Value, Is.EqualTo(277.026m));
            Assert.That(rows[4].FootnoteCodes, Is.EqualTo("R"));
        });
    }

    [Test]
    public async Task ParseCpiDataAsync_CommaDecimalCulture_ParsesValueInvariantly()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.data.0.Current.sample");
        var rows = new List<CpiData>();

        // A hand-built comma-decimal culture (like de-DE) so the test does not depend on ICU data.
        var commaDecimal = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        commaDecimal.NumberFormat.NumberDecimalSeparator = ",";
        commaDecimal.NumberFormat.NumberGroupSeparator = ".";

        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = commaDecimal;
        try
        {
            await foreach (var row in _parser.ParseCpiDataAsync(path))
                rows.Add(row);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        // Where '.' is a group separator, decimal.Parse turns "203.437" into 203437
        // unless the parse is pinned to the invariant culture.
        Assert.That(rows[2].Value, Is.EqualTo(203.437m));
    }

    [Test]
    public async Task ParseCpiDataAsync_RowWithoutFootnoteColumn_YieldsNullFootnote()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "series_id\tyear\tperiod\tvalue\nCUSR0000SA0\t1913\tM01\t9.800\n");
        try
        {
            var rows = new List<CpiData>();

            await foreach (var row in _parser.ParseCpiDataAsync(path))
                rows.Add(row);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].FootnoteCodes, Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
