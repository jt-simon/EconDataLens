namespace EconDataLens.Application.Cpi;

/// <summary>
///     Defines methods for ingesting Consumer Price Index (CPI) data from source files into the system.
/// </summary>
/// <remarks>
///     Each method handles a specific CPI reference data file (e.g. <c>cu.area</c>, <c>cu.item</c>,
///     <c>cu.data.0.Curernt</c>, etc).
///     Implementations of this interface are expected to download, parse, and persist the data from these files into the
///     database
///     but the details of persistence are left to the implementation.
/// </remarks>
public interface ICpiIngestionService
{
    /// <summary>
    ///     Imports CPI area definitions from the <c>cu.area</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportAreasAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports CPI footnotes from the <c>cu.footnote</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportFootnoteAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports CPI item definitions from teh <c>cu.item</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportItemAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI period definitions from the <c>cu.period</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportPeriodAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI series definitions from the <c>cu.series</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportSeriesAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI data points from the <c>cu.data.0.Current</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportCpiDataAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI periodicity definitions from the <c>cu.periodicity</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportPeriodicityAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI seasonal definitions from the <c>cu.seasonal</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportSeasonalAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI base definitions from the <c>cu.base</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportBaseAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the historical CPI data partitions (<c>cu.data.1</c> to <c>cu.data.20</c>) listed in
    ///     <c>HistoricalDataFiles</c>. Does nothing when none are configured.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportHistoricalCpiDataAsync(CancellationToken ct = default);

    /// <summary>
    ///     Imports the CPI aspects from the <c>cu.aspect</c> file.
    /// </summary>
    /// <param name="ct">Cancellation token to observe while awaiting results.</param>
    /// <returns>A task that completes when the import is finished.</returns>
    Task ImportAspectAsync(CancellationToken ct = default);
}