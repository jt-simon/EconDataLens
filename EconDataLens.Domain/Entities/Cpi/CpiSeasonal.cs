namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a seasonal adjustment type in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.seasonal</c> file provided by the Bureau of Labor Statistics (BLS).
/// </remarks>
public class CpiSeasonal
{
    /// <summary>
    /// Gets or sets the unique code.
    /// Matches the <c>seasonal_code</c> column in the BLS <c>cu.seasonal</c> file.
    /// </summary>
    public string SeasonalCode { get; set; } = null!;

    /// <summary>
    /// Gets or sets the descriptive text.
    /// Matches the <c>seasonal_text</c> column in the BLS <c>cu.seasonal</c> file.
    /// </summary>
    public string SeasonalText { get; set; } = null!;
}
