using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Application.Cpi;

/// <summary>
///     Contract for persisting and updating Consumer Price Index (CPI) reference and data entities.
/// </summary>
/// <remarks>
///     Upsert methods accept <see cref="IAsyncEnumerable{T}" /> to support streaming large datasets.
///     An <b>upsert</b> is defined as: insert when a row with the entity's natural key does not exist;
///     otherwise update the existing row. Implementations must be idempotent.
///     Transaction and batching semantics are implementation-specific but should ensure reasonable
///     atomicity and throughput for large loads. Cancellation should be honored promptly.
/// </remarks>
public interface ICpiIngestionRepository
{
    /// <summary>
    ///     Upserts a collection of CPI area definitions into the repository.
    /// </summary>
    /// <param name="areas">Stream of areas to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiAreaAsync(IAsyncEnumerable<CpiArea> areas, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI data points into the repository.
    /// </summary>
    /// <param name="cpiData">Stream of CPI data points to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiDataAsync(IAsyncEnumerable<CpiData> cpiData, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI footnotes into the repository.
    /// </summary>
    /// <param name="footnotes">Stream of footnotes to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiFootnotesAsync(IAsyncEnumerable<CpiFootnote> footnotes, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI item definitions into the repository.
    /// </summary>
    /// <param name="cpiItems">Stream of CPI items to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiItemAsync(IAsyncEnumerable<CpiItem> cpiItems, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI period definitions into the repository.
    /// </summary>
    /// <param name="periods">Stream of periods to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiPeriodAsync(IAsyncEnumerable<CpiPeriod> periods, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI series definitions into the repository.
    /// </summary>
    /// <param name="series">Stream of series to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiSeriesAsync(IAsyncEnumerable<CpiSeries> series, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI periodicity definitions into the repository.
    /// </summary>
    /// <param name="periodicitys">Stream of periodicity definitions to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiPeriodicityAsync(IAsyncEnumerable<CpiPeriodicity> periodicitys, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI seasonal definitions into the repository.
    /// </summary>
    /// <param name="seasonals">Stream of seasonal definitions to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiSeasonalAsync(IAsyncEnumerable<CpiSeasonal> seasonals, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI base definitions into the repository.
    /// </summary>
    /// <param name="bases">Stream of base definitions to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiBaseAsync(IAsyncEnumerable<CpiBase> bases, CancellationToken ct = default);

    /// <summary>
    ///     Upserts a collection of CPI aspects into the repository.
    /// </summary>
    /// <param name="aspects">Stream of aspects to persist.</param>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the upsert operation is finished.</returns>
    Task UpsertCpiAspectAsync(IAsyncEnumerable<CpiAspect> aspects, CancellationToken ct = default);
}