using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Infrastructure.Bls;

public class CpiDataFileParser
{

    public async IAsyncEnumerable<CpiArea> ParseCpiAreasAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Area file not found at path: {filePath}");

        await using var fs = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);

        using var sr = new StreamReader(
            fs,
            new UTF8Encoding(false),
            true,
            1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new FormatException($"Unexpected number of columns in CPI Area file line: {line}");

            yield return new CpiArea
            {
                AreaCode = parts[0].Trim(),
                AreaName = parts[1].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiData> ParseCpiDataAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Data file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);
        using var sr = new StreamReader(fs, new UTF8Encoding(false), true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');

            if (parts.Length < 4)
                throw new FormatException($"Unexpected number of columns in CPI Data file line: {line}");

            // BLS writes "-" when no value was published (e.g. footnote X, the 2025 lapse in appropriations).
            var rawValue = parts[3].Trim();

            yield return new CpiData
            {
                SeriesId = parts[0].Trim(),
                Year = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                Period = parts[2].Trim(),
                Value = rawValue == "-" ? null : decimal.Parse(rawValue, CultureInfo.InvariantCulture),
                FootnoteCodes = parts.Length > 4 && parts[4].Trim() != string.Empty ? parts[4].Trim() : null
            };
        }
    }

    public async IAsyncEnumerable<CpiFootnote> ParseCpiFootnoteAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Footnote file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);

        using var sr = new StreamReader(fs, new UTF8Encoding(false),
            true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new FormatException($"Unexpected number of columns in CPI Footnote file line: {line}");

            yield return new CpiFootnote
            {
                FootnoteCode = parts[0].Trim(),
                FootnoteText = parts[1].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiItem> ParseCpiItemsAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Item file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);
        using var sr = new StreamReader(fs, new UTF8Encoding(false),
            true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');

            if (parts.Length < 2)
                throw new FormatException($"Unexpected number of columns in CPI Item file line: {line}");

            yield return new CpiItem
            {
                ItemCode = parts[0].Trim(),
                ItemName = parts[1].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiPeriod> ParseCpiPeriodsAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Period file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);
        using var sr = new StreamReader(fs, new UTF8Encoding(false), true,
            1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');

            if (parts.Length < 3)
                throw new FormatException($"Unexpected number of columns in CPI Period file line: {line}");

            yield return new CpiPeriod
            {
                Period = parts[0].Trim(),
                PeriodAbbreviation = parts[1].Trim(),
                PeriodName = parts[2].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiSeries> ParseCpiSeriesAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Series file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);

        using var sr = new StreamReader(fs, new UTF8Encoding(false), true,
            1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');

            if (parts.Length < 13)
                throw new FormatException($"Unexpected number of columns in CPI Series file line: {line}");

            yield return new CpiSeries
            {
                SeriesId = parts[0].Trim(),
                AreaCode = parts[1].Trim(),
                ItemCode = parts[2].Trim(),
                Seasonal = parts[3].Trim(),
                PeriodicityCode = parts[4].Trim(),
                BaseCode = parts[5].Trim(),
                BasePeriod = parts[6].Trim(),
                SeriesTitle = parts[7].Trim(),
                FootnoteCodes = parts[8].Trim() == string.Empty ? null : parts[8].Trim(),
                BeginYear = int.Parse(parts[9].Trim(), CultureInfo.InvariantCulture),
                BeginPeriod = parts[10].Trim(),
                EndYear = int.Parse(parts[11].Trim(), CultureInfo.InvariantCulture),
                EndPeriod = parts[12].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiPeriodicity> ParseCpiPeriodicityAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Periodicity file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);

        using var sr = new StreamReader(fs, new UTF8Encoding(false),
            true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new FormatException($"Unexpected number of columns in CPI Periodicity file line: {line}");

            yield return new CpiPeriodicity
            {
                PeriodicityCode = parts[0].Trim(),
                PeriodicityName = parts[1].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiSeasonal> ParseCpiSeasonalAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Seasonal file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);

        using var sr = new StreamReader(fs, new UTF8Encoding(false),
            true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new FormatException($"Unexpected number of columns in CPI Seasonal file line: {line}");

            yield return new CpiSeasonal
            {
                SeasonalCode = parts[0].Trim(),
                SeasonalText = parts[1].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiBase> ParseCpiBaseAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Base file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);

        using var sr = new StreamReader(fs, new UTF8Encoding(false),
            true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length < 2)
                throw new FormatException($"Unexpected number of columns in CPI Base file line: {line}");

            yield return new CpiBase
            {
                BaseCode = parts[0].Trim(),
                BaseName = parts[1].Trim()
            };
        }
    }

    public async IAsyncEnumerable<CpiAspect> ParseCpiAspectAsync(string filePath, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"CPI Aspect file not found at path: {filePath}");

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            1 << 16, true);
        using var sr = new StreamReader(fs, new UTF8Encoding(false), true, 1 << 16);

        var isHeader = true;

        while (await sr.ReadLineAsync(ct) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var parts = line.Split('\t');

            if (parts.Length < 5)
                throw new FormatException($"Unexpected number of columns in CPI Aspect file line: {line}");

            yield return new CpiAspect
            {
                SeriesId = parts[0].Trim(),
                Year = int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                Period = parts[2].Trim(),
                AspectType = parts[3].Trim(),
                Value = parts[4].Trim(),
                FootnoteCodes = parts.Length > 5 && parts[5].Trim() != string.Empty ? parts[5].Trim() : null
            };
        }
    }
}