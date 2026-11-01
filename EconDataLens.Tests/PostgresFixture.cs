using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EconDataLens.Tests;

[SetUpFixture]
public class PostgresFixture
{
    private PostgreSqlContainer _container = default!;
    public static string ConnectionString { get; private set; } = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("econ_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder<EconDataLensDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var ctx = new EconDataLensDbContext(options);
        await ctx.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container.DisposeAsync();
    }
}