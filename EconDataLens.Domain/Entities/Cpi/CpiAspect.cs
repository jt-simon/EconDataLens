namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a per-observation aspect of a data point in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.aspect</c> file provided by the Bureau of Labor Statistics (BLS).
/// Aspects (available from 2012) describe a data point, e.g. percent changes or "highest since" markers.
/// BLS does not publish a mapping file for <see cref="AspectType"/>. Codes seen so far:
/// F, H1, HC, I, I1, M1, MC, V1, VC, W1, WC.
/// <see cref="Value"/> is kept as text because it mixes numbers (e.g. <c>0.03</c>) with markers
/// (e.g. <c>S-Jan. 2012</c>, <c>L-Apr.1975</c>).
/// </remarks>
public class CpiAspect
{
    /// <summary>
    /// Gets or sets the series identifier. Matches the <c>series_id</c> column in <c>cu.aspect</c>.
    /// </summary>
    public string SeriesId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the year of the observation. Matches the <c>year</c> column in <c>cu.aspect</c>.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the period of the observation. Matches the <c>period</c> column in <c>cu.aspect</c>.
    /// </summary>
    public string Period { get; set; } = null!;

    /// <summary>
    /// Gets or sets the aspect type code. Matches the <c>aspect_type</c> column in <c>cu.aspect</c>.
    /// </summary>
    public string AspectType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the aspect value as text. Matches the <c>value</c> column in <c>cu.aspect</c>.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the footnote codes. Matches the <c>footnote_codes</c> column in <c>cu.aspect</c>.
    /// </summary>
    public string? FootnoteCodes { get; set; }
}
