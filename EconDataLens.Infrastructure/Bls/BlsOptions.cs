namespace EconDataLens.Infrastructure.Bls;

/// <summary>
/// Strongly typed configuration options for retrieving Bureau of Labor Statistics (BLS) datasets.
/// </summary>
/// <remarks>
/// This class maps to the <c>BlsOptions</c> section of <c>appsettings.json</c>.
/// It can be registered in the DI container using <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
///
/// Example configuration section:
/// <code lang="json">
/// "BlsOptions": {
///   "CPI": {
///     "BaseUrl": "https://download.bls.gov/pub/time.series/cu/",
///     "DataFile": "cu.data.0.Current",
///     "AreaFile": "cu.area",
///     "FootnoteFile": "cu.footnote",
///     "ItemFile": "cu.item",
///     "PeriodFile": "cu.period",
///     "SeriesFile": "cu.series"
///   }
/// }
/// </code>
/// </remarks>
public class BlsOptions
{
    /// <summary>
    /// Gets or sets the User-Agent sent to BLS. BLS blocks anonymous and browser-spoofed agents, so this should
    /// identify the application and include contact info, e.g. <c>EconDataLens/1.0 (you@example.com)</c>.
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Consumer Price Index (CPI) specific configuration options
    /// such as file locations and metadata used in the CPI ETL process.
    ///
    /// See <see cref="CpiOptions"/> for details.
    /// </summary>
    public CpiOptions Cpi { get; set; } = new();
}