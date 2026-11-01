namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a data series in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.series</c> file provided by the Bureau of Labor Statistics (BLS).
/// It provides detailed metadata about each CPI data series, including area, item, periodicity, and time range.
/// </remarks>
public class CpiSeries
{
    /// <summary>
    /// Gets or sets the unique identifier for the CPI data series.
    /// Matches the <c>series_id</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string SeriesId { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the area code for the CPI data series.
    /// Matches the <c>area_code</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string AreaCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the item code for the CPI data series.
    /// Matches the <c>item_code</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string ItemCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the seasonal adjustment code for the CPI data series.
    /// Matches the <c>seasonal</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string Seasonal { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the periodicity code for the CPI data series.
    /// Matches the <c>periodicity_code</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string PeriodicityCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the base code for the CPI data series.
    /// Matches the <c>base_code</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string BaseCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the base period for the CPI data series.
    /// Matches the <c>base_period</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string BasePeriod { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the title of the CPI data series.
    /// Matches the <c>series_title</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string SeriesTitle { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the footnote codes associated with the CPI data series.
    /// Matches the <c>footnote_codes</c> column in the BLS <c>cu.series</c> file.
    /// This field may contain multiple footnote codes separated by spaces.
    /// </summary>
    public string? FootnoteCodes { get; set; }
    
    /// <summary>
    /// Gets or sets the beginning year of the CPI data series.
    /// Matches the <c>begin_year</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public int BeginYear { get; set; }
    
    /// <summary>
    /// Gets or sets the beginning period of the CPI data series.
    /// Matches the <c>begin_period</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public string BeginPeriod { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the ending year of the CPI data series.
    /// Matches the <c>end_year</c> column in the BLS <c>cu.series</c> file.
    /// </summary>
    public int EndYear { get; set; }
    
    /// <summary>
    /// Gets or sets the ending period of the CPI data series.
    /// Matches the <c>end_period</c> column in the BLS <c>>cu.series</c> file.
    /// </summary>
    public string EndPeriod { get; set; } = null!;
}