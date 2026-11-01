using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests;

public static class DbReset
{
    public static async Task RecreateDatabaseAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EconDataLensDbContext>()
        .UseNpgsql(connectionString)
        .Options;

        await using var ctx = new EconDataLensDbContext(options);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();
    }
}
