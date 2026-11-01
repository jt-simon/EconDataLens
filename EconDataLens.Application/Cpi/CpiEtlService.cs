namespace EconDataLens.Application.Cpi;

public class CpiEtlService : ICpiEtlService
{
    private readonly ICpiIngestionService _cpiIngestionService;

    public CpiEtlService(ICpiIngestionService cpiIngestionService)
    {
        _cpiIngestionService = cpiIngestionService;
    }

    public async Task RunEtlAsync(CancellationToken ct = default)
    {
        // Phase 1
        await _cpiIngestionService.ImportAreasAsync(ct);
        await _cpiIngestionService.ImportFootnoteAsync(ct);
        await _cpiIngestionService.ImportItemAsync(ct);
        await _cpiIngestionService.ImportPeriodAsync(ct);
        await _cpiIngestionService.ImportPeriodicityAsync(ct);
        await _cpiIngestionService.ImportSeasonalAsync(ct);
        await _cpiIngestionService.ImportBaseAsync(ct);

        // Phase 2 (Dependent on Phase 1):
        await _cpiIngestionService.ImportSeriesAsync(ct);

        // Phase 3 (Dependent on Phase 2):
        // Historical partitions first so the current file, which carries revisions, wins.
        await _cpiIngestionService.ImportHistoricalCpiDataAsync(ct);
        await _cpiIngestionService.ImportCpiDataAsync(ct);

        // Phase 4 (Dependent on Phase 2 and the period lookup):
        await _cpiIngestionService.ImportAspectAsync(ct);
    }
}
