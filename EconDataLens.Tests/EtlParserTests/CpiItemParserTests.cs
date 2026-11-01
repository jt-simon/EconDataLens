using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;

namespace EconDataLens.Tests.EtlParserTests;

public class CpiItemParserTests
{
    private CpiDataFileParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new CpiDataFileParser();
    }

    [Test]
    public async Task ParseCpiItemAsync_HeaderOnly_YieldsNoResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.item.empty");
        var rows = new List<CpiItem>();

        await foreach (var row in _parser.ParseCpiItemsAsync(path))
            rows.Add(row);

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiItemAsync_FileWithRecords_YieldsResults()
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.item.sample");
        var rows = new List<CpiItem>();

        await foreach (var row in _parser.ParseCpiItemsAsync(path))
            rows.Add(row);

        Assert.That(rows, Has.Count.EqualTo(3));

        Assert.Multiple(() =>
        {
            Assert.That(rows[0].ItemCode, Is.EqualTo("AA0"));
            Assert.That(rows[0].ItemName, Is.EqualTo("All items - old base"));
            Assert.That(rows[1].ItemCode, Is.EqualTo("SA0"));
            Assert.That(rows[1].ItemName, Is.EqualTo("All items"));
            Assert.That(rows[2].ItemCode, Is.EqualTo("SA0L12E"));
            Assert.That(rows[2].ItemName, Is.EqualTo("All items less food, shelter, and energy"));
        });
    }

}
