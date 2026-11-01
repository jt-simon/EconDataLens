using EconDataLens.Application.Cpi;
using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Tests.Application;

public class CpiEtlServiceTests
{
    private sealed class FakeSource : ICpiDataSource
    {
        private readonly int[] _historicalYears;
        private readonly int[] _currentYears;

        public FakeSource(int[]? historicalYears = null, int[]? currentYears = null)
        {
            _historicalYears = historicalYears ?? [];
            _currentYears = currentYears ?? [];
        }

        public Task<IAsyncEnumerable<CpiArea>> OpenAreasAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiArea>());
        public Task<IAsyncEnumerable<CpiFootnote>> OpenFootnotesAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiFootnote>());
        public Task<IAsyncEnumerable<CpiItem>> OpenItemsAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiItem>());
        public Task<IAsyncEnumerable<CpiPeriod>> OpenPeriodsAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiPeriod>());
        public Task<IAsyncEnumerable<CpiSeries>> OpenSeriesAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiSeries>());
        public Task<IAsyncEnumerable<CpiPeriodicity>> OpenPeriodicitiesAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiPeriodicity>());
        public Task<IAsyncEnumerable<CpiSeasonal>> OpenSeasonalsAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiSeasonal>());
        public Task<IAsyncEnumerable<CpiBase>> OpenBasesAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiBase>());
        public Task<IAsyncEnumerable<CpiData>> OpenDataAsync(CancellationToken ct = default) =>
            Task.FromResult(Rows(_currentYears));

        public Task<IAsyncEnumerable<CpiAspect>> OpenAspectsAsync(CancellationToken ct = default) =>
            Task.FromResult(Empty<CpiAspect>());

        public async IAsyncEnumerable<IAsyncEnumerable<CpiData>> OpenHistoricalDataAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            foreach (var year in _historicalYears)
                yield return Rows([year]);
        }

        private static async IAsyncEnumerable<CpiData> Rows(int[] years)
        {
            await Task.CompletedTask;
            foreach (var year in years)
                yield return new CpiData { SeriesId = "CUSR0000SA0", Year = year, Period = "M01", Value = 1m };
        }

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class RecordingRepository : ICpiIngestionRepository
    {
        public readonly List<string> Calls = new();
        public readonly List<int[]> DataBatches = new();
        public readonly List<CancellationToken> Tokens = new();

        public Task UpsertCpiAreaAsync(IAsyncEnumerable<CpiArea> areas, CancellationToken ct = default)
        {
            Calls.Add("Areas");
            Tokens.Add(ct);
            return Consume(areas, ct);
        }

        public Task UpsertCpiFootnotesAsync(IAsyncEnumerable<CpiFootnote> footnotes, CancellationToken ct = default)
        {
            Calls.Add("Footnotes");
            Tokens.Add(ct);
            return Consume(footnotes, ct);
        }

        public Task UpsertCpiItemAsync(IAsyncEnumerable<CpiItem> cpiItems, CancellationToken ct = default)
        {
            Calls.Add("Items");
            Tokens.Add(ct);
            return Consume(cpiItems, ct);
        }

        public Task UpsertCpiPeriodAsync(IAsyncEnumerable<CpiPeriod> periods, CancellationToken ct = default)
        {
            Calls.Add("Periods");
            Tokens.Add(ct);
            return Consume(periods, ct);
        }

        public Task UpsertCpiSeriesAsync(IAsyncEnumerable<CpiSeries> series, CancellationToken ct = default)
        {
            Calls.Add("Series");
            Tokens.Add(ct);
            return Consume(series, ct);
        }

        public Task UpsertCpiPeriodicityAsync(IAsyncEnumerable<CpiPeriodicity> periodicities, CancellationToken ct = default)
        {
            Calls.Add("Periodicities");
            Tokens.Add(ct);
            return Consume(periodicities, ct);
        }

        public Task UpsertCpiSeasonalAsync(IAsyncEnumerable<CpiSeasonal> seasonals, CancellationToken ct = default)
        {
            Calls.Add("Seasonals");
            Tokens.Add(ct);
            return Consume(seasonals, ct);
        }

        public Task UpsertCpiBaseAsync(IAsyncEnumerable<CpiBase> bases, CancellationToken ct = default)
        {
            Calls.Add("Bases");
            Tokens.Add(ct);
            return Consume(bases, ct);
        }

        public Task UpsertCpiAspectAsync(IAsyncEnumerable<CpiAspect> aspects, CancellationToken ct = default)
        {
            Calls.Add("Aspects");
            Tokens.Add(ct);
            return Consume(aspects, ct);
        }

        public Task UpsertCpiDataAsync(IAsyncEnumerable<CpiData> cpiData, CancellationToken ct = default)
        {
            Calls.Add("Data");
            Tokens.Add(ct);
            return Record(cpiData, ct);
        }

        private async Task Record(IAsyncEnumerable<CpiData> source, CancellationToken ct)
        {
            var years = new List<int>();
            await foreach (var d in source.WithCancellation(ct))
                years.Add(d.Year);
            DataBatches.Add(years.ToArray());
        }

        private static async Task Consume<T>(IAsyncEnumerable<T> source, CancellationToken ct)
        {
            await foreach (var _ in source.WithCancellation(ct))
            {
            }
        }
    }

    [Test]
    public async Task RunEtlAsync_UpsertsLookupsBeforeSeriesBeforeData()
    {
        var repo = new RecordingRepository();
        var ingestion = new CpiIngestionService(new FakeSource(), repo);
        var sut = new CpiEtlService(ingestion);

        await sut.RunEtlAsync();

        Assert.That(repo.Calls, Is.EqualTo(new[] { "Areas", "Footnotes", "Items", "Periods", "Periodicities", "Seasonals", "Bases", "Series", "Data", "Aspects" }));
    }

    [Test]
    public async Task RunEtlAsync_TokenSupplied_ForwardsItToEveryPhase()
    {
        var repo = new RecordingRepository();
        var sut = new CpiEtlService(new CpiIngestionService(new FakeSource(), repo));
        using var cts = new CancellationTokenSource();

        await sut.RunEtlAsync(cts.Token);

        Assert.That(repo.Tokens, Has.Count.EqualTo(10));
        Assert.That(repo.Tokens, Has.All.EqualTo(cts.Token));
    }

    [Test]
    public async Task RunEtlAsync_HistoricalPartitionsUpsertedOneAtATimeBeforeCurrent()
    {
        var repo = new RecordingRepository();
        var source = new FakeSource(historicalYears: [1913, 1914], currentYears: [2026]);
        var sut = new CpiEtlService(new CpiIngestionService(source, repo));

        await sut.RunEtlAsync();

        Assert.That(repo.DataBatches, Is.EqualTo(new[] { new[] { 1913 }, new[] { 1914 }, new[] { 2026 } }));
    }

    [Test]
    public async Task RunEtlAsync_NoHistoricalPartitionsConfigured_UpsertsOnlyCurrentData()
    {
        var repo = new RecordingRepository();
        var source = new FakeSource(currentYears: [2026]);
        var sut = new CpiEtlService(new CpiIngestionService(source, repo));

        await sut.RunEtlAsync();

        Assert.That(repo.DataBatches, Is.EqualTo(new[] { new[] { 2026 } }));
    }
}
