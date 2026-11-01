namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a period used in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.period</c> file provided by the Bureau of Labor Statistics (BLS).
/// It provides the period code, abbreviation, and descriptive name for CPI data series.
/// </remarks>
public class CpiPeriod
{
    /// <summary>
    /// Gets or sets the unique identifier for the CPI period.
    /// Matches the <c>period</c> column in the BLS <c>cu.period</c> file.
    /// </summary>
    public string Period { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the abbreviation of the CPI period.
    /// Matches the <c>period_abbreviation</c> column in the BLS <c>cu.period</c> file.
    /// </summary>
    public string PeriodAbbreviation { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the descriptive name of the CPI period.
    /// Matches the <c>period_name</c> column in the BLS <c>cu.period</c> file.
    /// </summary>
    public string PeriodName { get; set; } = null!;
}