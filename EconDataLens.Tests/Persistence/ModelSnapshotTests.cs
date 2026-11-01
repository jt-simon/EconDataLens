using EconDataLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EconDataLens.Tests.Persistence;

// Enforces the AGENTS.md "commit the snapshot" rule: the EF Core model must match
// the latest migration. No database is touched; the comparison is purely in memory.
public class ModelSnapshotTests
{
    [Test]
    public void Model_ComparedToLatestMigration_HasNoPendingChanges()
    {
        var opts = new DbContextOptionsBuilder<EconDataLensDbContext>()
            .UseNpgsql("Host=unused")
            .Options;

        using var db = new EconDataLensDbContext(opts);

        Assert.That(db.Database.HasPendingModelChanges(), Is.False,
            "The EF Core model drifted from the latest migration. " +
            "Generate one with: dotnet ef migrations add <Name> --project EconDataLens.Infrastructure --startup-project EconDataLens.Etl --output-dir Persistence/Migrations");
    }
}
