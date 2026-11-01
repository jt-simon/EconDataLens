using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EconDataLens.Infrastructure.Persistence;

/// <summary>
///     Design-time factory for creating <see cref="EconDataLensDbContext" /> instances.
/// </summary>
/// <remarks>
///     This factory is used by Entity Framework Core tools (e.g.,
///     <c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>)
///     to construct a DbContext when one cannot be created via dependency injection.
///     It resolves configuration from <c>appsettings.json</c>,
///     environment-specific overrides (<c>appsettings.&lt;Environment&gt;.json</c>),
///     and environment variables, then builds a context with Npgsql.
///     This class is not typically used at runtime.
/// </remarks>
public class EconDataLensDbContextFactory : IDesignTimeDbContextFactory<EconDataLensDbContext>
{
    public EconDataLensDbContext CreateDbContext(string[] args)
    {
        // Resolve configuration (supports appsettings, env vars, user secrets)
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{environment}.json", true)
            .AddEnvironmentVariables() // e.g. ConnectionStrings__Postgres
            .Build();

        // Connection string fallbacks
        var conn = config.GetConnectionString("Postgres");

        var options = new DbContextOptionsBuilder<EconDataLensDbContext>()
            .UseNpgsql(conn, npgsql => { })
            .Options;

        return new EconDataLensDbContext(options);
    }
}