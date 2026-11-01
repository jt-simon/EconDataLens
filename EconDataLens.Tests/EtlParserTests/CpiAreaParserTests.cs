using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;

namespace EconDataLens.Tests.EtlParserTests;

public class CpiAreaParserTests
{
    private CpiDataFileParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CpiDataFileParser();
    }

    [Test]
    public async Task ParseCpiAreaAsync_HeaderOnly_YieldsNoResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.area.empty");

        var rows = new List<CpiArea>();

        await foreach (var row in _parser.ParseCpiAreasAsync(path))
            rows.Add(row);

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiAreaAsync_FileWithRecords_YieldsResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.area.sample");

        var rows = new List<CpiArea>();

        await foreach (var row in _parser.ParseCpiAreasAsync(path))
            rows.Add(row);

        Assert.That(rows, Has.Count.EqualTo(4));

        Assert.Multiple(() =>
        {
            Assert.That(rows[0].AreaCode, Is.EqualTo("0000"));
            Assert.That(rows[0].AreaName, Is.EqualTo("U.S. city average"));

            Assert.That(rows[1].AreaCode, Is.EqualTo("0100"));
            Assert.That(rows[1].AreaName, Is.EqualTo("Northeast"));

            Assert.That(rows[2].AreaCode, Is.EqualTo("0110"));
            Assert.That(rows[2].AreaName, Is.EqualTo("New England"));

            Assert.That(rows[3].AreaCode, Is.EqualTo("0120"));
            Assert.That(rows[3].AreaName, Is.EqualTo("Middle Atlantic"));
        });
    }
}
