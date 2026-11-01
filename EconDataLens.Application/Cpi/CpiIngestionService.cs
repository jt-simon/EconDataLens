namespace EconDataLens.Application.Cpi;

public class CpiIngestionService : ICpiIngestionService
{
    private readonly ICpiDataSource _source;
    private readonly ICpiIngestionRepository _cpiIngestionRepository;

    public CpiIngestionService(
        ICpiDataSource source,
        ICpiIngestionRepository cpiIngestionRepository
    )
    {
        _source = source;
        _cpiIngestionRepository = cpiIngestionRepository;
    }

    /// <summary>
    ///     Downloads the CPI Area file from BLS, parses it, and upserts the data into the database.
    ///     Utilizes streaming to handle large files efficiently.
    /// </summary>
    public async Task ImportAreasAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiAreaAsync(await _source.OpenAreasAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI Footnote file from BLS, parses it, and upserts the data into the database.
    ///     Utilizes streaming to handle large files efficiently.
    /// </summary>
    public async Task ImportFootnoteAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiFootnotesAsync(await _source.OpenFootnotesAsync(ct), ct);
    }

    /// <summary>
    ///     Downlods the CPI Item file from BLS, parses it, and upserts the data into the database.
    ///     Utilizes streaming to handle large files efficiently.
    /// </summary>
    public async Task ImportItemAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiItemAsync(await _source.OpenItemsAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI Period file from BLS, parses it, and upserts the data into the database.
    ///     Utilizes streaming to handle large files efficiently.
    /// </summary>
    public async Task ImportPeriodAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiPeriodAsync(await _source.OpenPeriodsAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI Series file from BLS, parses it, and upserts the data
    ///     Utilizes streaming to handle large files efficiently
    /// </summary>
    public async Task ImportSeriesAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiSeriesAsync(await _source.OpenSeriesAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI data file from BLS, parses it, and upserts the data into the database.
    ///     Utilizes streaming to handle large files efficiently.
    /// </summary>
    public async Task ImportCpiDataAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiDataAsync(await _source.OpenDataAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI Periodicity file from BLS, parses it, and upserts the data into the database.
    /// </summary>
    public async Task ImportPeriodicityAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiPeriodicityAsync(await _source.OpenPeriodicitiesAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI Seasonal file from BLS, parses it, and upserts the data into the database.
    /// </summary>
    public async Task ImportSeasonalAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiSeasonalAsync(await _source.OpenSeasonalsAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads the CPI Base file from BLS, parses it, and upserts the data into the database.
    /// </summary>
    public async Task ImportBaseAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiBaseAsync(await _source.OpenBasesAsync(ct), ct);
    }

    /// <summary>
    ///     Downloads each configured historical data partition from BLS and upserts it into the database.
    ///     Partitions are upserted one at a time because they overlap on (series, year, period),
    ///     which a single INSERT ... ON CONFLICT cannot handle.
    /// </summary>
    public async Task ImportHistoricalCpiDataAsync(CancellationToken ct = default)
    {
        await foreach (var partition in _source.OpenHistoricalDataAsync(ct).WithCancellation(ct))
            await _cpiIngestionRepository.UpsertCpiDataAsync(partition, ct);
    }

    /// <summary>
    ///     Downloads the CPI Aspect file from BLS, parses it, and upserts the data into the database.
    ///     Utilizes streaming to handle large files efficiently.
    /// </summary>
    public async Task ImportAspectAsync(CancellationToken ct = default)
    {
        await _cpiIngestionRepository.UpsertCpiAspectAsync(await _source.OpenAspectsAsync(ct), ct);
    }
}
