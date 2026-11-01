namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents a footnote used in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.footnote</c> file provided by the Bureau of Labor Statistics (BLS).
/// It provides the footnote code and descriptive text for CPI data points.
/// </remarks>
public class CpiFootnote
{
    /// <summary>
    /// Gets or sets the unique identifier for the CPI footnote.
    /// Matches the <c>footnote_code</c> column in the BLS <c>cu.footnote</c> file.
    /// </summary>
    public string FootnoteCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the descriptive text of the CPI footnote.
    /// Matches the <c>footnote_text</c> column in the BLS <c>cu.footnote</c> file.
    /// </summary>
    public string FootnoteText { get; set; } = null!;
}