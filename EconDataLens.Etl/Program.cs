using EconDataLens.Application;
using EconDataLens.Application.Cpi;
using EconDataLens.Infrastructure;
using EconDataLens.Infrastructure.Bls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace EconDataLens.Etl;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Ctrl+C and SIGTERM (docker stop) cancel the run instead of killing the process mid-transaction.
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
        {
            context.Cancel = true;
            cts.Cancel();
        });

        return await RunAsync(CreateHostBuilder(args), cts.Token);
    }

    /// <summary>Builds the host with the application's real service registrations.</summary>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
            });

    /// <summary>
    ///     Runs the ETL on the given host builder and returns the process exit code (0, 1 or 2).
    ///     A cancelled run returns 1.
    /// </summary>
    public static async Task<int> RunAsync(IHostBuilder builder, CancellationToken ct = default)
    {
        IHost host;
        try
        {
            host = builder.Build();
            // Fail fast on invalid options (e.g. a blank User-Agent) before touching the database.
            _ = host.Services.GetRequiredService<IOptions<BlsOptions>>().Value;
        }
        catch (OptionsValidationException ex)
        {
            Console.WriteLine("❌ Invalid configuration:");
            Console.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Failed to build the host:");
            Console.WriteLine(ex);
            return 1;
        }

        using var disposableHost = host;

        Console.WriteLine("🔧 Starting ETL process...");

        try
        {
            await Infrastructure.DependencyInjection.MigrateDatabaseAsync(host.Services, ct);

            Console.WriteLine("✅ Database connection successful.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.WriteLine("⚠️ Cancelled while preparing the database.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Failed to connect to database:");
            Console.WriteLine(ex.Message);
            return 2;
        }

        try
        {
            using var scope = host.Services.CreateScope();
            var cpiEtlService = scope.ServiceProvider.GetRequiredService<ICpiEtlService>();

            Console.WriteLine("Running CPI ETL...");
            await cpiEtlService.RunEtlAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.WriteLine("⚠️ CPI ETL cancelled.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Failed to import CPI data files");
            Console.WriteLine(ex.Message);
            return 1;
        }

        return 0;
    }
}
