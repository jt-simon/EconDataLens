using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EconDataLens.Etl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EconDataLens.Tests.CompositionRoot;

// Runs the real composition root (Program.CreateHostBuilder / Program.RunAsync) end to end.
//
// The host's default configuration sources (appsettings.json copied into the test output,
// environment variables) are cleared and replaced by an in-memory collection, so the
// developer's or CI's environment can never leak into a test.
public class ProgramTests
{
    private const string UserAgent = "EconDataLens.Tests/1.0 (test@example.com)";

    private string _downloadDir = null!;

    [SetUp]
    public void SetUp()
    {
        _downloadDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_downloadDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_downloadDir, true);

    [Test]
    public async Task Main_DatabaseUnreachable_Returns2()
    {
        var exitCode = await RunAsync(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=1;Database=econdatalens;Username=x;Password=x;Timeout=2",
            ["BlsOptions:UserAgent"] = UserAgent
        });

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public async Task Main_MalformedConnectionString_Returns2()
    {
        var exitCode = await RunAsync(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Bogus=1",
            ["BlsOptions:UserAgent"] = UserAgent
        });

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public async Task Main_HostBuildFails_Returns1AndPrintsFullException()
    {
        var builder = Program.CreateHostBuilder([])
            .ConfigureAppConfiguration((_, config) => config.Sources.Clear())
            .UseDefaultServiceProvider(o => o.ValidateOnBuild = true)
            .ConfigureServices(services => services.AddSingleton<NeedsMissingService>());

        var (exitCode, output) = await CaptureConsoleAsync(() => Program.RunAsync(builder));

        // The full exception (type and stack), not just its message, so a non-configuration
        // failure can be diagnosed and is not mislabelled as a configuration problem.
        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(output, Does.Contain("AggregateException"));
            Assert.That(output, Does.Not.Contain("Invalid configuration"));
        });
    }

    [Test]
    public async Task Main_BlankUserAgentWithDownloadEnabled_FailsBeforeTouchingDatabase()
    {
        // The database is unreachable: reaching the migrate step would return 2, so exit code 1
        // proves the User-Agent was rejected before the database was contacted.
        var exitCode = await RunAsync(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=1;Database=econdatalens;Username=x;Password=x;Timeout=2",
            ["DownloadOptions:SkipDownload"] = "false"
        });

        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public async Task Main_BlankUserAgentWithSkipDownload_PassesValidationAndReachesDatabase()
    {
        var exitCode = await RunAsync(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=1;Database=econdatalens;Username=x;Password=x;Timeout=2",
            ["DownloadOptions:SkipDownload"] = "true"
        });

        Assert.That(exitCode, Is.EqualTo(2));
    }

    [Test]
    public async Task Main_BlankUserAgentWithDownloadEnabled_Returns1AndNamesSetting()
    {
        await DbReset.RecreateDatabaseAsync(PostgresFixture.ConnectionString);

        var settings = CpiFileSettings();
        settings["ConnectionStrings:Postgres"] = PostgresFixture.ConnectionString;
        settings["DownloadOptions:DownloadDirectory"] = _downloadDir;
        settings["DownloadOptions:SkipDownload"] = "false";
        settings["BlsOptions:Cpi:BaseUrl"] = "https://example.invalid/";

        var (exitCode, output) = await CaptureConsoleAsync(() => RunAsync(settings));

        // The guard fires before any request is sent, so the output names the setting.
        Assert.Multiple(() =>
        {
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(output, Does.Contain("BlsOptions:UserAgent"));
        });
    }

    [Test]
    public async Task Main_AllFilesPresentLocally_Returns0AndLoadsEveryPhase()
    {
        var settings = await LocalFilesSettingsAsync();

        var exitCode = await RunAsync(settings);

        Assert.That(exitCode, Is.EqualTo(0));

        var opts = new DbContextOptionsBuilder<EconDataLensDbContext>()
            .UseNpgsql(PostgresFixture.ConnectionString)
            .Options;
        await using var db = new EconDataLensDbContext(opts);

        Assert.Multiple(() =>
        {
            Assert.That(db.CpiData.Count(), Is.EqualTo(4), "cpi_data rows");
            Assert.That(db.CpiAspect.Count(), Is.EqualTo(2), "cpi_aspect rows (last phase ran)");
        });
    }

    [Test]
    public async Task Main_CancelledBeforeRun_Returns1WithoutLoading()
    {
        // The same configuration as the successful run, which returns 0; only the token differs.
        var settings = await LocalFilesSettingsAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var exitCode = await RunAsync(settings, cts.Token);

        Assert.That(exitCode, Is.EqualTo(1));
    }

    private async Task<Dictionary<string, string?>> LocalFilesSettingsAsync()
    {
        await DbReset.RecreateDatabaseAsync(PostgresFixture.ConnectionString);

        // Copy the verbatim samples under the real BLS file names so SkipDownload resolves them.
        var sourceDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "EtlData");
        foreach (var (_, blsName, sampleName) in BlsFileSamples)
            File.Copy(Path.Combine(sourceDir, sampleName), Path.Combine(_downloadDir, blsName));

        var settings = CpiFileSettings();
        settings["ConnectionStrings:Postgres"] = PostgresFixture.ConnectionString;
        settings["DownloadOptions:DownloadDirectory"] = _downloadDir;
        settings["DownloadOptions:SkipDownload"] = "true";
        settings["BlsOptions:UserAgent"] = UserAgent;
        settings["BlsOptions:Cpi:BaseUrl"] = "https://download.bls.gov/pub/time.series/cu/";
        return settings;
    }

    // (CpiOptions key, real BLS file, TestData/EtlData sample)
    private static readonly (string OptionKey, string BlsName, string Sample)[] BlsFileSamples =
    [
        ("DataFile", "cu.data.0.Current", "cu.data.0.Current.sample"),
        ("AreaFile", "cu.area", "cu.area.sample"),
        ("FootnoteFile", "cu.footnote", "cu.footnote.sample"),
        ("ItemFile", "cu.item", "cu.item.sample"),
        ("PeriodFile", "cu.period", "cu.period.sample"),
        ("SeriesFile", "cu.series", "cu.series.sample"),
        ("PeriodicityFile", "cu.periodicity", "cu.periodicity.sample"),
        ("SeasonalFile", "cu.seasonal", "cu.seasonal.sample"),
        ("BaseFile", "cu.base", "cu.base.sample"),
        ("AspectFile", "cu.aspect", "cu.aspect.sample")
    ];

    private static Dictionary<string, string?> CpiFileSettings() =>
        BlsFileSamples.ToDictionary(f => $"BlsOptions:Cpi:{f.OptionKey}", f => (string?)f.BlsName);

    private static async Task<(int ExitCode, string Output)> CaptureConsoleAsync(Func<Task<int>> run)
    {
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            return (await run(), writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // Registered with ValidateOnBuild to make the host fail to build: its dependency is never registered.
    private interface IMissingDependency;

    private sealed class NeedsMissingService(IMissingDependency dependency)
    {
        public IMissingDependency Dependency { get; } = dependency;
    }

    private static Task<int> RunAsync(IDictionary<string, string?> settings, CancellationToken ct = default)
    {
        var builder = Program.CreateHostBuilder([])
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(settings);
            });

        return Program.RunAsync(builder, ct);
    }
}
