using System.Runtime.CompilerServices;
using EconDataLens.Application.Cpi;
using EconDataLens.Domain.Entities.Cpi;
using Microsoft.Extensions.Options;

namespace EconDataLens.Infrastructure.Bls;

public class BlsCpiDataSource : ICpiDataSource
{
    private readonly BasicFileDownloadService _downloader;
    private readonly CpiDataFileParser _parser;
    private readonly BlsOptions _blsOptions;
    private readonly DownloadOptions _downloadOptions;

    public BlsCpiDataSource(
        BasicFileDownloadService downloader,
        CpiDataFileParser parser,
        IOptions<BlsOptions> blsOptions,
        IOptions<DownloadOptions> downloadOptions)
    {
        _downloader = downloader;
        _parser = parser;
        _blsOptions = blsOptions.Value;
        _downloadOptions = downloadOptions.Value;
    }

    public async Task<IAsyncEnumerable<CpiArea>> OpenAreasAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.AreaFile, ct);
        return _parser.ParseCpiAreasAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiFootnote>> OpenFootnotesAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.FootnoteFile, ct);
        return _parser.ParseCpiFootnoteAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiItem>> OpenItemsAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.ItemFile, ct);
        return _parser.ParseCpiItemsAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiPeriod>> OpenPeriodsAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.PeriodFile, ct);
        return _parser.ParseCpiPeriodsAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiSeries>> OpenSeriesAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.SeriesFile, ct);
        return _parser.ParseCpiSeriesAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiData>> OpenDataAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.DataFile, ct);
        return _parser.ParseCpiDataAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiPeriodicity>> OpenPeriodicitiesAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.PeriodicityFile, ct);
        return _parser.ParseCpiPeriodicityAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiSeasonal>> OpenSeasonalsAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.SeasonalFile, ct);
        return _parser.ParseCpiSeasonalAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiBase>> OpenBasesAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.BaseFile, ct);
        return _parser.ParseCpiBaseAsync(path, ct);
    }

    public async Task<IAsyncEnumerable<CpiAspect>> OpenAspectsAsync(CancellationToken ct = default)
    {
        var path = await DownloadAsync(_blsOptions.Cpi.AspectFile, ct);
        return _parser.ParseCpiAspectAsync(path, ct);
    }

    public async IAsyncEnumerable<IAsyncEnumerable<CpiData>> OpenHistoricalDataAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var file in _blsOptions.Cpi.HistoricalDataFiles)
        {
            var path = await DownloadAsync(file, ct);
            yield return _parser.ParseCpiDataAsync(path, ct);
        }
    }

    private Task<string> DownloadAsync(string file, CancellationToken ct)
    {
        var url = _blsOptions.Cpi.BaseUrl + file;
        var path = Path.Combine(_downloadOptions.DownloadDirectory, file);

        if (_downloadOptions.SkipDownload)
            return File.Exists(path)
                ? Task.FromResult(Path.GetFullPath(path))
                : throw new FileNotFoundException(
                    $"SkipDownload is enabled but {file} was not found in the download directory: {path}");

        return _downloader.DownloadFileAsync(url, path, ct);
    }
}
