using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Infrastructure.Bls;
using Microsoft.Extensions.Options;

namespace EconDataLens.Tests.EtlDataSourceTests;

public class BlsCpiDataSourceTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_dir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, true);

    private const string AreaFile =
        "area_code\tarea_name\tdisplay_level\tselectable\tsort_sequence\n0000\tU.S. city average\t0\tT\t1\n";

    private BlsCpiDataSource CreateSource(CpiOptions cpi, bool skipDownload, RecordingHandler handler,
        string userAgent = "EconDataLens.Tests/1.0 (test@example.com)")
    {
        var bls = Options.Create(new BlsOptions { UserAgent = userAgent, Cpi = cpi });
        var download = Options.Create(new DownloadOptions { DownloadDirectory = _dir, SkipDownload = skipDownload });

        return new BlsCpiDataSource(new BasicFileDownloadService(new HttpClient(handler), bls), new CpiDataFileParser(), bls, download);
    }

    [Test]
    public async Task OpenAreasAsync_SkipDownloadWithExistingFile_ParsesLocalFileWithoutHttp()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "cu.area"), AreaFile);
        var handler = new RecordingHandler();

        var areas = new List<CpiArea>();
        await foreach (var a in await CreateSource(new CpiOptions { BaseUrl = "https://example.invalid/", AreaFile = "cu.area" }, true, handler).OpenAreasAsync())
            areas.Add(a);

        Assert.That(areas, Has.Count.EqualTo(1));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task OpenAreasAsync_SkipDownloadWithBlankUserAgent_ParsesLocalFile()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "cu.area"), AreaFile);

        var areas = new List<CpiArea>();
        await foreach (var a in await CreateSource(
            new CpiOptions { BaseUrl = "https://example.invalid/", AreaFile = "cu.area" },
            true, new RecordingHandler(), userAgent: "").OpenAreasAsync())
        {
            areas.Add(a);
        }

        Assert.That(areas, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task OpenAreasAsync_SkipDownloadWithMalformedUserAgent_ParsesLocalFile()
    {
        await File.WriteAllTextAsync(Path.Combine(_dir, "cu.area"), AreaFile);

        // An unbalanced parenthesis is not a valid User-Agent header value; it must not matter
        // while no request is sent.
        var areas = new List<CpiArea>();
        await foreach (var a in await CreateSource(
            new CpiOptions { BaseUrl = "https://example.invalid/", AreaFile = "cu.area" },
            true, new RecordingHandler(), userAgent: "EconDataLens (a@b.com").OpenAreasAsync())
        {
            areas.Add(a);
        }

        Assert.That(areas, Has.Count.EqualTo(1));
    }

    [Test]
    public void OpenAreasAsync_SkipDownloadWithMissingFile_ThrowsNamingTheFileWithoutHttp()
    {
        var handler = new RecordingHandler();

        var ex = Assert.ThrowsAsync<FileNotFoundException>(() =>
            CreateSource(new CpiOptions { BaseUrl = "https://example.invalid/", AreaFile = "cu.area" }, true, handler).OpenAreasAsync());

        Assert.That(ex!.Message, Does.Contain("cu.area"));
        Assert.That(handler.Requests, Is.Empty);
    }

    // Each Open*Async must fetch exactly its own configured file; parsing is lazy,
    // so calling the open alone triggers the download and nothing is parsed.
    private static IEnumerable<TestCaseData> OpenMethods()
    {
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenAreasAsync()), new Action<CpiOptions, string>((o, f) => o.AreaFile = f), "cu.area")
            .SetName("OpenAreasAsync_DownloadEnabled_RequestsBaseUrlPlusAreaFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenFootnotesAsync()), new Action<CpiOptions, string>((o, f) => o.FootnoteFile = f), "cu.footnote")
            .SetName("OpenFootnotesAsync_DownloadEnabled_RequestsBaseUrlPlusFootnoteFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenItemsAsync()), new Action<CpiOptions, string>((o, f) => o.ItemFile = f), "cu.item")
            .SetName("OpenItemsAsync_DownloadEnabled_RequestsBaseUrlPlusItemFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenPeriodsAsync()), new Action<CpiOptions, string>((o, f) => o.PeriodFile = f), "cu.period")
            .SetName("OpenPeriodsAsync_DownloadEnabled_RequestsBaseUrlPlusPeriodFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenSeriesAsync()), new Action<CpiOptions, string>((o, f) => o.SeriesFile = f), "cu.series")
            .SetName("OpenSeriesAsync_DownloadEnabled_RequestsBaseUrlPlusSeriesFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenDataAsync()), new Action<CpiOptions, string>((o, f) => o.DataFile = f), "cu.data.0.Current")
            .SetName("OpenDataAsync_DownloadEnabled_RequestsBaseUrlPlusDataFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenPeriodicitiesAsync()), new Action<CpiOptions, string>((o, f) => o.PeriodicityFile = f), "cu.periodicity")
            .SetName("OpenPeriodicitiesAsync_DownloadEnabled_RequestsBaseUrlPlusPeriodicityFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenSeasonalsAsync()), new Action<CpiOptions, string>((o, f) => o.SeasonalFile = f), "cu.seasonal")
            .SetName("OpenSeasonalsAsync_DownloadEnabled_RequestsBaseUrlPlusSeasonalFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenBasesAsync()), new Action<CpiOptions, string>((o, f) => o.BaseFile = f), "cu.base")
            .SetName("OpenBasesAsync_DownloadEnabled_RequestsBaseUrlPlusBaseFile");
        yield return new TestCaseData(
            new Func<BlsCpiDataSource, Task>(s => s.OpenAspectsAsync()), new Action<CpiOptions, string>((o, f) => o.AspectFile = f), "cu.aspect")
            .SetName("OpenAspectsAsync_DownloadEnabled_RequestsBaseUrlPlusAspectFile");
    }

    [TestCaseSource(nameof(OpenMethods))]
    public async Task OpenXAsync_DownloadEnabled_RequestsBaseUrlPlusConfiguredFile(
        Func<BlsCpiDataSource, Task> open, Action<CpiOptions, string> setFile, string fileName)
    {
        var cpi = new CpiOptions { BaseUrl = "https://example.invalid/" };
        setFile(cpi, fileName);
        var handler = new RecordingHandler();
        var source = CreateSource(cpi, false, handler);

        await open(source);

        Assert.That(handler.Requests, Is.EqualTo(["https://example.invalid/" + fileName]));
    }

    [Test]
    public async Task OpenAreasAsync_DownloadEnabled_ParsesDownloadedBody()
    {
        var handler = new RecordingHandler { Content = new StringContent(AreaFile) };

        var areas = new List<CpiArea>();
        await foreach (var a in await CreateSource(
            new CpiOptions { BaseUrl = "https://example.invalid/", AreaFile = "cu.area" },
            false, handler).OpenAreasAsync())
        {
            areas.Add(a);
        }

        // The download landed in the download directory, exactly where the parser reads it.
        Assert.Multiple(() =>
        {
            Assert.That(areas, Has.Count.EqualTo(1));
            Assert.That(areas[0].AreaCode, Is.EqualTo("0000"));
            Assert.That(areas[0].AreaName, Is.EqualTo("U.S. city average"));
        });
    }

    [Test]
    public async Task OpenHistoricalDataAsync_TwoFilesConfigured_YieldsPartitionsInConfiguredOrder()
    {
        // Same (series, year, period) in both partitions; the first file has the old value
        // and the second the revised one, so a reversed order would feed the wrong winner to the caller.
        const string fileA = "partition_a.cu.data";
        const string fileB = "partition_b.cu.data";
        await File.WriteAllTextAsync(Path.Combine(_dir, fileA),
            "series_id\tyear\tperiod\tvalue\nCUSR0000SA0\t2000\tM01\t111\n");
        await File.WriteAllTextAsync(Path.Combine(_dir, fileB),
            "series_id\tyear\tperiod\tvalue\nCUSR0000SA0\t2000\tM01\t222\n");

        var partitions = new List<List<CpiData>>();
        await foreach (var partition in CreateSource(
            new CpiOptions
            {
                BaseUrl = "https://example.invalid/",
                HistoricalDataFiles = [fileA, fileB]
            },
            true, new RecordingHandler()).OpenHistoricalDataAsync())
        {
            var rows = new List<CpiData>();
            await foreach (var row in partition)
                rows.Add(row);
            partitions.Add(rows);
        }

        Assert.That(partitions, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(partitions[0], Has.Count.EqualTo(1));
            Assert.That(partitions[1], Has.Count.EqualTo(1));
            // The configured order is preserved: first partition yields 111, second the revised 222.
            Assert.That(partitions[0][0].Value, Is.EqualTo(111m));
            Assert.That(partitions[1][0].Value, Is.EqualTo(222m));
        });
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        public HttpContent? Content { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = Content ?? new StringContent(AreaFile)
            });
        }
    }
}
