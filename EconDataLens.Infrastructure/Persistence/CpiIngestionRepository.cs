using System.Data;
using EconDataLens.Domain.Entities.Cpi;
using EconDataLens.Application.Cpi;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace EconDataLens.Infrastructure.Persistence;

public class CpiIngestionRepository : ICpiIngestionRepository
{
    private readonly EconDataLensDbContext _dbContext;

    public CpiIngestionRepository(EconDataLensDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertCpiAreaAsync(IAsyncEnumerable<CpiArea> areas, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            // temp table
            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_area (
                                                       area_code VARCHAR(4) PRIMARY KEY, 
                                                       area_name VARCHAR(80) NOT NULL
                                                     ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // SINGLE COPY session, streamed
            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_area (area_code, area_name) FROM STDIN (FORMAT BINARY)", ct))
            {
                await foreach (var a in areas.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(a.AreaCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(a.AreaName, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // one upsert
            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_area (area_code, area_name)
                                                        SELECT area_code, area_name FROM tmp_cpi_area
                                                        ON CONFLICT (area_code) DO UPDATE SET area_name = EXCLUDED.area_name;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }


    public async Task UpsertCpiDataAsync(IAsyncEnumerable<CpiData> cpiData, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_data (
                                                     series_id VARCHAR(17), 
                                                     year INT,
                                                     period VARCHAR(3),
                                                     value NUMERIC(18, 3) NULL,
                                                     footnote_codes VARCHAR(10) NULL
                                                     ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_data (series_id, year, period, value, footnote_codes) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                await foreach (var d in cpiData.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(d.SeriesId, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(d.Year, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(d.Period, NpgsqlDbType.Text, ct);
                    if (d.Value.HasValue)
                        await writer.WriteAsync(d.Value.Value, NpgsqlDbType.Numeric, ct);
                    else
                        await writer.WriteAsync(DBNull.Value, NpgsqlDbType.Numeric, ct);

                    if (d.FootnoteCodes != null)
                        await writer.WriteAsync(d.FootnoteCodes, NpgsqlDbType.Text, ct);
                    else
                        await writer.WriteAsync(DBNull.Value, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // one upsert
            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_data (series_id, year, period, value, footnote_codes)
                                                        SELECT series_id, year, period, value, footnote_codes FROM tmp_cpi_data
                                                        ON CONFLICT (series_id, year, period) DO UPDATE SET
                                                        value = EXCLUDED.value,
                                                        footnote_codes = EXCLUDED.footnote_codes;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiFootnotesAsync(IAsyncEnumerable<CpiFootnote> footnotes, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            // temp table
            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_footnote (
                                                        footnote_code VARCHAR(1) PRIMARY KEY,
                                                        footnote_text VARCHAR(100) NOT NULL
                                                        ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_footnote (footnote_code, footnote_text) FROM STDIN (FORMAT BINARY)", ct))
            {
                await foreach (var f in footnotes.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(f.FootnoteCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(f.FootnoteText, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // one upsert
            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_footnote (footnote_code, footnote_text)
                                                        SELECT footnote_code, footnote_text FROM tmp_cpi_footnote
                                                        ON CONFLICT (footnote_code) DO UPDATE SET footnote_text = EXCLUDED.footnote_text;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiItemAsync(IAsyncEnumerable<CpiItem> cpiItems, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            // temp table
            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_item (
                                                       item_code VARCHAR(8) PRIMARY KEY, 
                                                       item_name VARCHAR(100) NOT NULL
                                                        ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_item (item_code, item_name) FROM STDIN (FORMAT BINARY)", ct))
            {
                await foreach (var i in cpiItems.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(i.ItemCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(i.ItemName, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // one upsert
            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_item (item_code, item_name)
                                                        SELECT item_code, item_name FROM tmp_cpi_item
                                                        ON CONFLICT (item_code) DO UPDATE SET item_name = EXCLUDED.item_name;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiPeriodAsync(IAsyncEnumerable<CpiPeriod> periods, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_period (
                                                     period VARCHAR(3) PRIMARY KEY,
                                                     period_abbreviation VARCHAR(5) NOT NULL,
                                                     period_name VARCHAR(20) NOT NULL 
                                                     ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_period (period, period_abbreviation, period_name) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                await foreach (var p in periods.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(p.Period, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(p.PeriodAbbreviation, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(p.PeriodName, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // one upsert
            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_period (period, period_abbreviation, period_name)
                                                        SELECT period, period_abbreviation, period_name FROM tmp_cpi_period
                                                        ON CONFLICT (period) DO UPDATE SET 
                                                          period_abbreviation = EXCLUDED.period_abbreviation,
                                                          period_name = EXCLUDED.period_name;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiSeriesAsync(IAsyncEnumerable<CpiSeries> series, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            // temp table
            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_series (
                                                     series_id VARCHAR(17),
                                                     area_code VARCHAR(4),
                                                     item_code VARCHAR(8),
                                                     seasonal VARCHAR(1),
                                                     periodicity_code VARCHAR(1),
                                                     base_code VARCHAR(1),
                                                     base_period VARCHAR(20),
                                                     series_title TEXT,
                                                     footnote_codes VARCHAR(12) NULL,
                                                     begin_year INT,
                                                     begin_period VARCHAR(3),
                                                     end_year INT,
                                                     end_period VARCHAR(3)
                                                     ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_series (series_id, area_code, item_code, seasonal, periodicity_code, base_code, base_period, series_title, footnote_codes, begin_year, begin_period, end_year, end_period) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                await foreach (var s in series.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(s.SeriesId, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.AreaCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.ItemCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.Seasonal, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.PeriodicityCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.BaseCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.BasePeriod, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.SeriesTitle, NpgsqlDbType.Text, ct);

                    if (s.FootnoteCodes != null)
                        await writer.WriteAsync(s.FootnoteCodes, NpgsqlDbType.Text, ct);
                    else
                        await writer.WriteAsync(DBNull.Value, NpgsqlDbType.Text, ct);

                    await writer.WriteAsync(s.BeginYear, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(s.BeginPeriod, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(s.EndYear, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(s.EndPeriod, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            // one upsert
            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_series (series_id, area_code, item_code, seasonal, periodicity_code, base_code, base_period, series_title, footnote_codes, begin_year, begin_period, end_year, end_period)
                                                        SELECT series_id, area_code, item_code, seasonal, periodicity_code, base_code, base_period, series_title, footnote_codes, begin_year, begin_period, end_year, end_period
                                                        FROM tmp_cpi_series
                                                        ON CONFLICT (series_id) DO UPDATE SET 
                                                          area_code = EXCLUDED.area_code,
                                                          item_code = EXCLUDED.item_code,
                                                          seasonal = EXCLUDED.seasonal,
                                                          periodicity_code = EXCLUDED.periodicity_code,
                                                          base_code = EXCLUDED.base_code,
                                                          base_period = EXCLUDED.base_period,
                                                          series_title = EXCLUDED.series_title,
                                                          footnote_codes = EXCLUDED.footnote_codes,
                                                          begin_year = EXCLUDED.begin_year,
                                                          begin_period = EXCLUDED.begin_period,
                                                          end_year = EXCLUDED.end_year,
                                                          end_period = EXCLUDED.end_period;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiPeriodicityAsync(IAsyncEnumerable<CpiPeriodicity> periodicitys, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_periodicity (
                                                        periodicity_code VARCHAR(1) PRIMARY KEY,
                                                        periodicity_name VARCHAR(20) NOT NULL
                                                        ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_periodicity (periodicity_code, periodicity_name) FROM STDIN (FORMAT BINARY)", ct))
            {
                await foreach (var r in periodicitys.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(r.PeriodicityCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(r.PeriodicityName, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_periodicity (periodicity_code, periodicity_name)
                                                        SELECT periodicity_code, periodicity_name FROM tmp_cpi_periodicity
                                                        ON CONFLICT (periodicity_code) DO UPDATE SET periodicity_name = EXCLUDED.periodicity_name;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiSeasonalAsync(IAsyncEnumerable<CpiSeasonal> seasonals, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_seasonal (
                                                        seasonal_code VARCHAR(1) PRIMARY KEY,
                                                        seasonal_text VARCHAR(30) NOT NULL
                                                        ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_seasonal (seasonal_code, seasonal_text) FROM STDIN (FORMAT BINARY)", ct))
            {
                await foreach (var r in seasonals.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(r.SeasonalCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(r.SeasonalText, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_seasonal (seasonal_code, seasonal_text)
                                                        SELECT seasonal_code, seasonal_text FROM tmp_cpi_seasonal
                                                        ON CONFLICT (seasonal_code) DO UPDATE SET seasonal_text = EXCLUDED.seasonal_text;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiBaseAsync(IAsyncEnumerable<CpiBase> bases, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_base (
                                                        base_code VARCHAR(1) PRIMARY KEY,
                                                        base_name VARCHAR(20) NOT NULL
                                                        ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_base (base_code, base_name) FROM STDIN (FORMAT BINARY)", ct))
            {
                await foreach (var r in bases.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(r.BaseCode, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(r.BaseName, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_base (base_code, base_name)
                                                        SELECT base_code, base_name FROM tmp_cpi_base
                                                        ON CONFLICT (base_code) DO UPDATE SET base_name = EXCLUDED.base_name;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }

    public async Task UpsertCpiAspectAsync(IAsyncEnumerable<CpiAspect> aspects, CancellationToken ct = default)
    {
        var conn = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var cmd = new NpgsqlCommand("""
                                                     CREATE TEMP TABLE tmp_cpi_aspect (
                                                     series_id VARCHAR(17),
                                                     year INT,
                                                     period VARCHAR(3),
                                                     aspect_type VARCHAR(2),
                                                     value VARCHAR(16),
                                                     footnote_codes VARCHAR(10) NULL
                                                     ) ON COMMIT DROP;
                                                     """, conn, tx))
            {
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await using (var writer = await conn.BeginBinaryImportAsync(
                             "COPY tmp_cpi_aspect (series_id, year, period, aspect_type, value, footnote_codes) FROM STDIN (FORMAT BINARY)",
                             ct))
            {
                await foreach (var a in aspects.WithCancellation(ct))
                {
                    await writer.StartRowAsync(ct);
                    await writer.WriteAsync(a.SeriesId, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(a.Year, NpgsqlDbType.Integer, ct);
                    await writer.WriteAsync(a.Period, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(a.AspectType, NpgsqlDbType.Text, ct);
                    await writer.WriteAsync(a.Value, NpgsqlDbType.Text, ct);

                    if (a.FootnoteCodes != null)
                        await writer.WriteAsync(a.FootnoteCodes, NpgsqlDbType.Text, ct);
                    else
                        await writer.WriteAsync(DBNull.Value, NpgsqlDbType.Text, ct);
                }

                await writer.CompleteAsync(ct);
            }

            await using (var upsert = new NpgsqlCommand("""
                                                        INSERT INTO cpi_aspect (series_id, year, period, aspect_type, value, footnote_codes)
                                                        SELECT series_id, year, period, aspect_type, value, footnote_codes FROM tmp_cpi_aspect
                                                        ON CONFLICT (series_id, year, period, aspect_type) DO UPDATE SET
                                                        value = EXCLUDED.value,
                                                        footnote_codes = EXCLUDED.footnote_codes;
                                                        """, conn, tx))
            {
                await upsert.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        });

        _dbContext.ChangeTracker.Clear();
    }
}