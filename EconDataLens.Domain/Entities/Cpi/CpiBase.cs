namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a base period type (current or alternate) in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.base</c> file provided by the Bureau of Labor Statistics (BLS).
/// </remarks>
public class CpiBase
{
    /// <summary>
    /// Gets or sets the unique code.
    /// Matches the <c>base_code</c> column in the BLS <c>cu.base</c> file.
    /// </summary>
    public string BaseCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the descriptive text.
    /// Matches the <c>base_name</c> column in the BLS <c>cu.base</c> file.
    /// </summary>
    public string BaseName { get; set; } = null!;
}
