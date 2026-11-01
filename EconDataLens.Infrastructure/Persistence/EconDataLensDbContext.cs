using EconDataLens.Domain.Entities.Cpi;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Infrastructure.Persistence;

/// <summary>
///     Entity Framework Core database context for EconDataLens.
/// </summary>
/// <remarks>
///     This context configures the schema for EconDataLens, including entities for Consumer Price Index (CPI) data.
///     Customizations:
///     <list type="bullet">
///         <item>
///             <description>Snake_case naming convention for tables/columns.</description>
///         </item>
///         <item>
///             <description>Composite primary key on <see cref="CpiData" /> (SeriesId, Year, Period).</description>
///         </item>
///         <item>
///             <description>Foreign keys defined for series, items, areas, and periods.</description>
///         </item>
///         <item>
///             <description>Some fields (e.g., footnote codes) are stored as comma-separated lists and not normalized.</description>
///         </item>
///     </list>
/// </remarks>
public class EconDataLensDbContext : DbContext
{
    public EconDataLensDbContext(DbContextOptions<EconDataLensDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<CpiArea> CpiArea { get; set; }
    public DbSet<CpiData> CpiData { get; set; }
    public DbSet<CpiFootnote> CpiFootnote { get; set; }
    public DbSet<CpiItem> CpiItem { get; set; }
    public DbSet<CpiPeriod> CpiPeriod { get; set; }
    public DbSet<CpiSeries> CpiSeries { get; set; }
    public DbSet<CpiPeriodicity> CpiPeriodicity { get; set; }
    public DbSet<CpiSeasonal> CpiSeasonal { get; set; }
    public DbSet<CpiBase> CpiBase { get; set; }
    public DbSet<CpiAspect> CpiAspect { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Snake case naming convention
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CpiArea>().HasKey(x => x.AreaCode);
        modelBuilder.Entity<CpiArea>().Property(x => x.AreaCode).HasMaxLength(4);
        modelBuilder.Entity<CpiArea>().Property(x => x.AreaName).HasMaxLength(80);

        modelBuilder.Entity<CpiFootnote>().HasKey(x => x.FootnoteCode);
        modelBuilder.Entity<CpiFootnote>().Property(x => x.FootnoteCode).HasMaxLength(1);
        modelBuilder.Entity<CpiFootnote>().Property(x => x.FootnoteText).HasMaxLength(100);

        modelBuilder.Entity<CpiItem>().HasKey(x => x.ItemCode);
        modelBuilder.Entity<CpiItem>().Property(x => x.ItemCode).HasMaxLength(8);
        modelBuilder.Entity<CpiItem>().Property(x => x.ItemName).HasMaxLength(100);

        modelBuilder.Entity<CpiPeriod>().HasKey(x => x.Period);
        modelBuilder.Entity<CpiPeriod>().Property(x => x.Period).HasMaxLength(3);
        modelBuilder.Entity<CpiPeriod>().Property(x => x.PeriodAbbreviation).HasMaxLength(5);
        modelBuilder.Entity<CpiPeriod>().Property(x => x.PeriodName).HasMaxLength(20);

        modelBuilder.Entity<CpiPeriodicity>().HasKey(x => x.PeriodicityCode);
        modelBuilder.Entity<CpiPeriodicity>().Property(x => x.PeriodicityCode).HasMaxLength(1);
        modelBuilder.Entity<CpiPeriodicity>().Property(x => x.PeriodicityName).HasMaxLength(20);

        modelBuilder.Entity<CpiSeasonal>().HasKey(x => x.SeasonalCode);
        modelBuilder.Entity<CpiSeasonal>().Property(x => x.SeasonalCode).HasMaxLength(1);
        modelBuilder.Entity<CpiSeasonal>().Property(x => x.SeasonalText).HasMaxLength(30);

        modelBuilder.Entity<CpiBase>().HasKey(x => x.BaseCode);
        modelBuilder.Entity<CpiBase>().Property(x => x.BaseCode).HasMaxLength(1);
        modelBuilder.Entity<CpiBase>().Property(x => x.BaseName).HasMaxLength(20);

        // CPI Series
        // Note: Don't FK to CpiFootnote since it's a comma-separated list TODO deal with this later
        // Note: Don't FK BasePeriod to CpiPeriod since it's not a period code 
        modelBuilder.Entity<CpiSeries>().HasKey(x => x.SeriesId);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiArea>().WithMany().HasForeignKey(x => x.AreaCode)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiItem>().WithMany().HasForeignKey(x => x.ItemCode)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiPeriodicity>().WithMany().HasForeignKey(x => x.PeriodicityCode)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiSeasonal>().WithMany().HasForeignKey(x => x.Seasonal)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiBase>().WithMany().HasForeignKey(x => x.BaseCode)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiPeriod>().WithMany().HasForeignKey(x => x.BeginPeriod)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().HasOne<CpiPeriod>().WithMany().HasForeignKey(x => x.EndPeriod)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiSeries>().Property(x => x.FootnoteCodes).IsRequired(false);
        modelBuilder.Entity<CpiSeries>().Property(x => x.SeriesId).HasMaxLength(17);
        modelBuilder.Entity<CpiSeries>().Property(x => x.AreaCode).HasMaxLength(4);
        modelBuilder.Entity<CpiSeries>().Property(x => x.ItemCode).HasMaxLength(8);
        modelBuilder.Entity<CpiSeries>().Property(x => x.Seasonal).HasMaxLength(1);
        modelBuilder.Entity<CpiSeries>().Property(x => x.PeriodicityCode).HasMaxLength(1);
        modelBuilder.Entity<CpiSeries>().Property(x => x.BaseCode).HasMaxLength(1);
        modelBuilder.Entity<CpiSeries>().Property(x => x.BasePeriod).HasMaxLength(20);
        modelBuilder.Entity<CpiSeries>().Property(x => x.BeginYear).HasMaxLength(4);
        modelBuilder.Entity<CpiSeries>().Property(x => x.BeginPeriod).HasMaxLength(3);
        modelBuilder.Entity<CpiSeries>().Property(x => x.EndYear).HasMaxLength(4);
        modelBuilder.Entity<CpiSeries>().Property(x => x.EndPeriod).HasMaxLength(3);
        modelBuilder.Entity<CpiSeries>().Property(x => x.FootnoteCodes).HasMaxLength(12);

        // CPI Data
        // Note: Don't FK to CpiFootnote since it's a comma-separated list TODO deal with this later
        modelBuilder.Entity<CpiData>().HasKey(x => new { x.SeriesId, x.Year, x.Period });
        modelBuilder.Entity<CpiData>().HasOne<CpiSeries>().WithMany().HasForeignKey(x => x.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiData>().HasOne<CpiPeriod>().WithMany().HasForeignKey(x => x.Period)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiData>().Property(x => x.Value).HasPrecision(18, 3);
        modelBuilder.Entity<CpiData>().Property(x => x.FootnoteCodes).IsRequired(false);
        modelBuilder.Entity<CpiData>().Property(x => x.SeriesId).HasMaxLength(17);
        modelBuilder.Entity<CpiData>().Property(x => x.Year).HasMaxLength(4);
        modelBuilder.Entity<CpiData>().Property(x => x.Period).HasMaxLength(3);
        modelBuilder.Entity<CpiData>().Property(x => x.Value).HasMaxLength(12);
        modelBuilder.Entity<CpiData>().Property(x => x.FootnoteCodes).HasMaxLength(10);

        // CPI Aspect
        // Note: Value is text; it mixes numbers with markers like "S-Jan. 2012".
        // Note: Don't FK AspectType; BLS publishes no mapping file for it.
        modelBuilder.Entity<CpiAspect>().HasKey(x => new { x.SeriesId, x.Year, x.Period, x.AspectType });
        modelBuilder.Entity<CpiAspect>().HasOne<CpiSeries>().WithMany().HasForeignKey(x => x.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiAspect>().HasOne<CpiPeriod>().WithMany().HasForeignKey(x => x.Period)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CpiAspect>().Property(x => x.FootnoteCodes).IsRequired(false);
        modelBuilder.Entity<CpiAspect>().Property(x => x.SeriesId).HasMaxLength(17);
        modelBuilder.Entity<CpiAspect>().Property(x => x.Period).HasMaxLength(3);
        modelBuilder.Entity<CpiAspect>().Property(x => x.AspectType).HasMaxLength(2);
        modelBuilder.Entity<CpiAspect>().Property(x => x.Value).HasMaxLength(16);
        modelBuilder.Entity<CpiAspect>().Property(x => x.FootnoteCodes).HasMaxLength(10);
    }
}