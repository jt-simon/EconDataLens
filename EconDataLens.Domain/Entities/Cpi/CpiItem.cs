namespace EconDataLens.Domain.Entities.Cpi;

/// <summary>
/// Represents an item used in the Consumer Price Index (CPI) dataset.
/// </summary>
/// <remarks>
/// This entity corresponds to the records in the <c>cu.item</c> file provided by the Bureau of Labor Statistics (BLS).
/// It provides the item code and descriptive name for CPI data series.
/// </remarks>
public class CpiItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the CPI item.
    /// Matches the <c>item_code</c> column in the BLS <c>cu.item</c> file.
    /// </summary>
    public string ItemCode { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the descriptive name of the CPI item.
    /// Matches the <c>item_name</c> column in the BLS <c>cu.item</c> file.
    /// </summary>
    public string ItemName { get; set; } = null!;
}