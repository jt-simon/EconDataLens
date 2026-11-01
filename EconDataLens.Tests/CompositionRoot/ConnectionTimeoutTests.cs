using EconDataLens.Infrastructure;
using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EconDataLens.Tests.CompositionRoot;

public class ConnectionTimeoutTests
{
    private const string Base = "Host=localhost;Database=x;Username=x;Password=x";

    // The bulk upserts use raw NpgsqlCommands, which take their timeout from the connection string.
    // The 30s default cancels the ~1M-row cpi_data upsert mid-statement.
    [TestCase(Base, 600, TestName = "DefaultTimeout_IsRaisedForBulkUpserts")]
    [TestCase(Base + ";Command Timeout=0", 0, TestName = "ExplicitNoTimeout_IsKept")]
    [TestCase(Base + ";Command Timeout=900", 900, TestName = "ExplicitLongerTimeout_IsKept")]
    public void CommandTimeout_ForConnectionString_IsAsExpected(string connectionString, int expected)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = connectionString })
            .Build();
        using var provider = new ServiceCollection().AddInfrastructure(config).BuildServiceProvider();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EconDataLensDbContext>();
        var csb = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());

        Assert.That(csb.CommandTimeout, Is.EqualTo(expected));
    }
}
