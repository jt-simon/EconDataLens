using Microsoft.Extensions.Options;

namespace EconDataLens.Infrastructure.Bls;

public class BasicFileDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly string _userAgent;

    internal const string MissingUserAgentMessage =
        "BlsOptions:UserAgent must be set to an identifying value with contact info, e.g. \"EconDataLens/1.0 (you@example.com)\".";

    public BasicFileDownloadService(HttpClient httpClient, IOptions<BlsOptions> options)
    {
        _httpClient = httpClient;

        _httpClient.Timeout = TimeSpan.FromMinutes(10);

        // BLS rejects generic/browser-like agents; it asks automated clients to identify themselves with contact info.
        // Kept as a string and applied per request: a blank or malformed value must only matter
        // when a download is attempted, so SkipDownload runs need no valid User-Agent.
        _userAgent = options.Value.UserAgent;
    }

    public async Task<string> DownloadFileAsync(string url, string destinationPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_userAgent))
            throw new InvalidOperationException(MissingUserAgentMessage);

        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException(nameof(url));

        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

        destinationPath = Path.GetFullPath(destinationPath);
        var dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // Unique per call so concurrent downloads of the same file cannot delete or truncate each other's temp file.
        var tempPath = $"{destinationPath}.{Guid.NewGuid():N}.partial";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(_userAgent);
        using var resp = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        resp.EnsureSuccessStatusCode();

        try
        {
            await using (var httpStream = await resp.Content.ReadAsStreamAsync(ct))
            await using (var fileStream =
                         new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                await httpStream.CopyToAsync(fileStream, ct);
                await fileStream.FlushAsync(ct);
            }

            if (File.Exists(destinationPath))
                File.Replace(tempPath, destinationPath, null, true);
            else
                File.Move(tempPath, destinationPath);
        }
        finally
        {
            // A failed or cancelled download must not leave a truncated temp file behind.
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        return destinationPath;
    }
}
