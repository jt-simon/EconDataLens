using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Application.Cpi;

/// <summary>
/// Source of CPI reference and data streams. Downloads first, then returns a lazy stream,
/// so the network fetch stays outside the repository's COPY transaction.
/// </summary>
public interface ICpiDataSource
{
    Task<IAsyncEnumerable<CpiArea>> OpenAreasAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiFootnote>> OpenFootnotesAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiItem>> OpenItemsAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiPeriod>> OpenPeriodsAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiSeries>> OpenSeriesAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiData>> OpenDataAsync(CancellationToken ct = default);

    Task<IAsyncEnumerable<CpiAspect>> OpenAspectsAsync(CancellationToken ct = default);

    /// <summary>
    /// Yields one stream per configured historical data partition. Each partition is downloaded
    /// when the outer sequence advances to it, so consumers should upsert each one before moving on.
    /// Yields nothing when no partitions are configured.
    /// </summary>
    IAsyncEnumerable<IAsyncEnumerable<CpiData>> OpenHistoricalDataAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiPeriodicity>> OpenPeriodicitiesAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiSeasonal>> OpenSeasonalsAsync(CancellationToken ct = default);
    Task<IAsyncEnumerable<CpiBase>> OpenBasesAsync(CancellationToken ct = default);
}
