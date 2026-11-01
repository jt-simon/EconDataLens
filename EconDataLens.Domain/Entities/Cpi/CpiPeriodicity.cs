namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a data frequency (e.g. monthly, semi-annual) in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.periodicity</c> file provided by the Bureau of Labor Statistics (BLS).
/// </remarks>
public class CpiPeriodicity
{
    /// <summary>
    /// Gets or sets the unique code.
    /// Matches the <c>periodicity_code</c> column in the BLS <c>cu.periodicity</c> file.
    /// </summary>
    public string PeriodicityCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the descriptive text.
    /// Matches the <c>periodicity_name</c> column in the BLS <c>cu.periodicity</c> file.
    /// </summary>
    public string PeriodicityName { get; set; } = null!;
}
