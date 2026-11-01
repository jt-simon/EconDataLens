using EconDataLens.Infrastructure.Bls;
using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Tests.EtlParserTests;

[TestFixture]
public class CpiFootnoteParserTests
{
    [SetUp]
    public void SetUp()
    {
        _parser = new CpiDataFileParser();
    }

    private CpiDataFileParser _parser = null!;

    [Test]
    public async Task ParseCpiFootnoteAsync_HeaderOnly_YieldsNoResults()
    {
        // Arrange
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.footnote.empty");

        // Act
        var rows = new List<CpiFootnote>();

        await foreach (var row in _parser.ParseCpiFootnoteAsync(path))
            rows.Add(row);

        // Assert
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task ParseCpiFootnoteAsync_FileWithRecords_YieldsResults()
    {
        // Arrange
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "ParserData", "cu.footnote.sample");

        // Act
        var rows = new List<CpiFootnote>();

        await foreach (var row in _parser.ParseCpiFootnoteAsync(path))
            rows.Add(row);

        // Assert
        // Sample file has 2 data rows plus header
        Assert.Multiple(() =>
        {
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].FootnoteCode, Is.EqualTo("R"));
            Assert.That(rows[0].FootnoteText, Is.EqualTo("Revised"));
            Assert.That(rows[1].FootnoteCode, Is.EqualTo("X"));
            Assert.That(rows[1].FootnoteText, Is.EqualTo("Data unavailable due to the 2025 lapse in appropriations"));
        });
    }
}
