namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a single data point in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.data.0.Current</c> file provided by the Bureau of Labor Statistics (BLS).
/// </remarks>
public class CpiData
{
    /// <summary>
    /// Gets or sets the unique identifier for the CPI data series.
    /// Matches the <c>series_id</c> column in the BLS <c>cu.data.0.Current</c> file.
    /// </summary>
    public string SeriesId { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the year of the CPI data point.
    /// Matches the <c>year</c> column in the BLS <c>>cu.data.0.Current</c> file.
    /// </summary>
    public int Year { get; set; }
    
    /// <summary>
    /// Gets or sets the period of the CPI data point (e.g. "M01" for January).
    /// Matches the <c>period</c> column in the BLS <c>>cu.data.0.Current</c> file.
    /// </summary>
    public string Period { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the CPI value for the specified series, year, and period.
    /// Matches the <c>value</c> column in the BLS <c>>cu.data.0.Current</c> file.
    /// <c>null</c> when BLS published no value (written as <c>-</c>, e.g. footnote <c>X</c>).
    /// </summary>
    public decimal? Value { get; set; }
    
    /// <summary>
    /// Gets or sets any footnote codes associated with this CPI data point.
    /// Matches the <c>footnote_codes</c> column in the BLS <c>>cu.data.0.Current</c> file.
    /// </summary>
    public string? FootnoteCodes { get; set; }
}