namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a geographic area used in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.area</c> file provided by the Bureau of Labor Statistics (BLS).
/// It provides the area code and human-readable area name for CPI data series.
/// </remarks>
public class CpiArea
{
    /// <summary>
    /// Gets or sets the unique identifier for the CPI area.
    /// Matches the <c>area_code</c> column in the BLS <c>cu.area</c> file.
    /// </summary>
    public string AreaCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the descriptive name of the CPI area.
    /// Matches the <c>area_name</c> column in the BLS <c>cu.area</c> file.
    /// </summary>
    public string AreaName { get; set; } = null!;
}