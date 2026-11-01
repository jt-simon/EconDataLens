using System.Net;
using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure.Bls;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EconDataLens.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Parsed lazily: a malformed connection string fails when the DbContext is first
        // resolved (the migrate step, exit code 2), not while the host is being built.
        services.AddDbContext<EconDataLensDbContext>(options =>
            options.UseNpgsql(WithBulkLoadTimeout(configuration.GetConnectionString("Postgres"))));

        services.Configure<DownloadOptions>(configuration.GetSection("DownloadOptions"));
        // Checked by Program before the database is touched. A blank User-Agent only matters
        // when files are downloaded, so SkipDownload runs are exempt.
        services.AddOptions<BlsOptions>()
            .Bind(configuration.GetSection("BlsOptions"))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.UserAgent) || configuration.GetValue<bool>("DownloadOptions:SkipDownload"),
                BasicFileDownloadService.MissingUserAgentMessage);

        services.AddHttpClient<BasicFileDownloadService>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

        services.AddScoped<ICpiIngestionRepository, CpiIngestionRepository>();
        services.AddScoped<CpiDataFileParser>();
        services.AddScoped<ICpiDataSource, BlsCpiDataSource>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconDataLensDbContext>();
        await db.Database.MigrateAsync(ct);
    }

    // The bulk upserts run raw NpgsqlCommands, which take their timeout from the connection string.
    // The 30s default cancels the ~1M-row cpi_data upsert, so raise it unless the caller set 0 (none) or more.
    private static string? WithBulkLoadTimeout(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;

        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        if (csb.CommandTimeout is > 0 and < BulkLoadCommandTimeoutSeconds)
            csb.CommandTimeout = BulkLoadCommandTimeoutSeconds;

        return csb.ConnectionString;
    }

    private const int BulkLoadCommandTimeoutSeconds = 600;
}
