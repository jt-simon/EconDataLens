using System.Net;
using EconDataLens.Infrastructure.Bls;
using Microsoft.Extensions.Options;

namespace EconDataLens.Tests.EtlDataSourceTests;

// Coverage for BasicFileDownloadService: the User-Agent guard (BLS's main 403 cause),
// atomic .partial writes, and error/partial-failure handling of the destination file.
public class BasicFileDownloadServiceTests
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

    private const string UserAgent = "EconDataLens.Tests/1.0 (test@example.com)";

    private BasicFileDownloadService CreateService(StubDownloadHandler handler, string userAgent = UserAgent)
    {
        var options = Options.Create(new BlsOptions { UserAgent = userAgent });
        return new BasicFileDownloadService(new HttpClient(handler), options);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void DownloadFileAsync_BlankUserAgent_ThrowsWithoutSendingRequest(string userAgent)
    {
        var handler = new StubDownloadHandler();
        var service = CreateService(handler, userAgent);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DownloadFileAsync("https://example.invalid/cu.area", Path.Combine(_dir, "cu.area")));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Contain("BlsOptions:UserAgent"));
            Assert.That(handler.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task DownloadFileAsync_AnyRequest_SendsConfiguredUserAgent()
    {
        var handler = new StubDownloadHandler();
        var service = CreateService(handler);

        await service.DownloadFileAsync("https://example.invalid/cu.area", Path.Combine(_dir, "cu.area"));

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        Assert.That(handler.Requests[0].UserAgent, Is.EqualTo(UserAgent));
    }

    [Test]
    public async Task DownloadFileAsync_Success_WritesBodyAndLeavesNoPartialFile()
    {
        var handler = new StubDownloadHandler { Content = new StringContent("header\nrow") };
        var destination = Path.Combine(_dir, "nested", "cu.area");

        var result = await CreateService(handler).DownloadFileAsync("https://example.invalid/cu.area", destination);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(Path.GetFullPath(result), Is.EqualTo(destination));
            Assert.That(await File.ReadAllTextAsync(destination), Is.EqualTo("header\nrow"));
            Assert.That(Directory.GetFiles(_dir, "*.partial", SearchOption.AllDirectories), Is.Empty);
        });
    }

    [Test]
    public async Task DownloadFileAsync_DestinationExists_ReplacesWithNewContent()
    {
        var destination = Path.Combine(_dir, "cu.area");
        await File.WriteAllTextAsync(destination, "old");
        var handler = new StubDownloadHandler { Content = new StringContent("new") };
        var service = CreateService(handler);

        await service.DownloadFileAsync("https://example.invalid/cu.area", destination);

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await File.ReadAllTextAsync(destination), Is.EqualTo("new"));
            Assert.That(Directory.GetFiles(_dir, "*.partial", SearchOption.AllDirectories), Is.Empty);
            Assert.That(Directory.GetFiles(_dir), Is.EqualTo(new[] { destination }), "no .backup or other leftovers");
        });
    }

    // 304 is included on purpose: no conditional headers are ever sent, so it is just another unexpected status.
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.NotModified)]
    public async Task DownloadFileAsync_ErrorStatus_ThrowsAndKeepsExistingFile(HttpStatusCode status)
    {
        var destination = Path.Combine(_dir, "cu.area");
        await File.WriteAllTextAsync(destination, "old");
        var handler = new StubDownloadHandler { StatusCode = status };
        var service = CreateService(handler);

        Assert.ThrowsAsync<HttpRequestException>(
            () => service.DownloadFileAsync("https://example.invalid/cu.area", destination));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await File.ReadAllTextAsync(destination), Is.EqualTo("old"));
            Assert.That(Directory.GetFiles(_dir, "*.partial", SearchOption.AllDirectories), Is.Empty);
        });
    }

    [Test]
    public async Task DownloadFileAsync_BodyFailsMidStream_KeepsExistingFileAndRemovesPartial()
    {
        var destination = Path.Combine(_dir, "cu.area");
        await File.WriteAllTextAsync(destination, "old");
        var handler = new StubDownloadHandler
        {
            Content = new StreamContent(new ThrowingStream("prefix"u8.ToArray()))
        };
        var service = CreateService(handler);

        Assert.ThrowsAsync<IOException>(
            () => service.DownloadFileAsync("https://example.invalid/cu.area", destination));

        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await File.ReadAllTextAsync(destination), Is.EqualTo("old"));
            Assert.That(Directory.GetFiles(_dir, "*.partial", SearchOption.AllDirectories), Is.Empty);
        });
    }

    private sealed class StubDownloadHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

        public HttpContent? Content { get; set; }

        public List<(Uri? Uri, string UserAgent)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add((request.RequestUri, request.Headers.UserAgent.ToString()));
            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                Content = Content ?? new StringContent(string.Empty)
            });
        }
    }

    // Serves the prefix bytes on the first read, then fails like a dropped connection mid-body.
    private sealed class ThrowingStream(byte[] prefix) : MemoryStream(prefix)
    {
        private long _reads;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (_reads++ >= 1)
                throw new IOException("Simulated mid-stream failure.");

            return base.ReadAsync(buffer, ct);
        }
    }
}
