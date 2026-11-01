using EconDataLens.Infrastructure.Bls;
using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Tests.EtlParserTests;

public class CpiPeriodParserTests
{
    private CpiDataFileParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CpiDataFileParser();
    }

    [Test]
    public async Task ParseCpiPeriodAsync_HeaderOnly_YieldsNoResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.period.empty");
        var rows = new List<CpiPeriod>();

        await foreach (var row in _parser.ParseCpiPeriodsAsync(path))
            rows.Add(row);

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiPeriodAsync_FileWithRecords_YieldsResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.period.sample");
        var rows = new List<CpiPeriod>();

        await foreach (var row in _parser.ParseCpiPeriodsAsync(path))
            rows.Add(row);

        Assert.That(rows, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Period, Is.EqualTo("M01"));
            Assert.That(rows[0].PeriodAbbreviation, Is.EqualTo("JAN"));
            Assert.That(rows[0].PeriodName, Is.EqualTo("January"));

            // M13 and S03 both use the abbreviation "AN AV"
            Assert.That(rows[1].Period, Is.EqualTo("M13"));
            Assert.That(rows[1].PeriodAbbreviation, Is.EqualTo("AN AV"));
            Assert.That(rows[1].PeriodName, Is.EqualTo("Annual Average"));

            Assert.That(rows[2].Period, Is.EqualTo("S01"));
            Assert.That(rows[2].PeriodAbbreviation, Is.EqualTo("HALF1"));
            Assert.That(rows[2].PeriodName, Is.EqualTo("First Half"));

            Assert.That(rows[3].Period, Is.EqualTo("S03"));
            Assert.That(rows[3].PeriodAbbreviation, Is.EqualTo("AN AV"));
            Assert.That(rows[3].PeriodName, Is.EqualTo("Annual Average"));
        });
    }

}
