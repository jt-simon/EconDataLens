namespace EconDataLens.Application.Cpi;

/// <summary>
///     Defines methods for executing the ETL (Extract, Transform, Load) process for Consumer Price Index (CPI) data.
/// </summary>
/// <remarks>
///     This interface outlines the contract for services that handle the ETL operations specific to CPI datasets,
///     including data extraction from source files, transformation of data into the desired format, and loading into
///     the target data store.
/// </remarks>
public interface ICpiEtlService
{
    /// <summary>
    ///     This method orchestrates the complete ETL process for Consumer Price Index (CPI) data.
    /// </summary>
    /// <param name="ct">Cancels the run; checked while downloading, parsing and loading.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunEtlAsync(CancellationToken ct = default);
}