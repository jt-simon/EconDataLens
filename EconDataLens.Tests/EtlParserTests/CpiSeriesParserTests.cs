using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;

namespace EconDataLens.Tests.EtlParserTests;

public class CpiSeriesParserTests
{
    private CpiDataFileParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CpiDataFileParser();
    }

    [Test]
    public async Task ParseCpiSeriesAsync_HeaderOnly_YieldsNoResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.series.empty");
        var rows = new List<CpiSeries>();

        await foreach (var row in _parser.ParseCpiSeriesAsync(path))
            rows.Add(row);

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiSeriesAsync_FileWithRecords_YieldsResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.series.sample");
        var rows = new List<CpiSeries>();

        await foreach (var row in _parser.ParseCpiSeriesAsync(path))
            rows.Add(row);

        Assert.That(rows, Has.Count.EqualTo(3));

        Assert.Multiple(() =>
        {
            Assert.That(rows[0].SeriesId, Is.EqualTo("CUSR0000SA0"));
            Assert.That(rows[0].AreaCode, Is.EqualTo("0000"));
            Assert.That(rows[0].ItemCode, Is.EqualTo("SA0"));
            Assert.That(rows[0].Seasonal, Is.EqualTo("S"));
            Assert.That(rows[0].PeriodicityCode, Is.EqualTo("R"));
            Assert.That(rows[0].BaseCode, Is.EqualTo("S"));
            Assert.That(rows[0].BasePeriod, Is.EqualTo("1982-84=100"));
            Assert.That(rows[0].SeriesTitle, Is.EqualTo("All items in U.S. city average, all urban consumers, seasonally adjusted"));
            Assert.That(rows[0].FootnoteCodes, Is.Null);
            Assert.That(rows[0].BeginYear, Is.EqualTo(1947));
            Assert.That(rows[0].BeginPeriod, Is.EqualTo("M01"));
            Assert.That(rows[0].EndYear, Is.EqualTo(2026));
            Assert.That(rows[0].EndPeriod, Is.EqualTo("M09"));
        });
    }

    [Test]
    public async Task ParseCpiSeriesAsync_SemiannualAlternateBase_ParsesFields()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.series.sample");
        var rows = new List<CpiSeries>();

        await foreach (var row in _parser.ParseCpiSeriesAsync(path))
            rows.Add(row);

        var series = rows.Single(r => r.SeriesId == "CUUS0000AA0");

        Assert.Multiple(() =>
        {
            Assert.That(series.Seasonal, Is.EqualTo("U"));
            Assert.That(series.PeriodicityCode, Is.EqualTo("S"));
            Assert.That(series.BaseCode, Is.EqualTo("A"));
            Assert.That(series.BasePeriod, Is.EqualTo("1967=100"));
            Assert.That(series.BeginYear, Is.EqualTo(1984));
            Assert.That(series.BeginPeriod, Is.EqualTo("S01"));
            Assert.That(series.EndYear, Is.EqualTo(2026));
            Assert.That(series.EndPeriod, Is.EqualTo("S01"));
        });
    }
}
