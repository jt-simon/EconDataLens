using EconDataLens.Domain.Entities.Cpi;

namespace EconDataLens.Tests.Architecture;

public class DependencyRuleTests
{
    [Test]
    public void Domain_References_NoEconDataLensOrMicrosoftAssemblies()
    {
        var referenced = typeof(CpiArea).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(referenced.Where(n => n.StartsWith("EconDataLens.")), Is.Empty,
                $"Domain must not reference EconDataLens assemblies: {string.Join(", ", referenced)}");
            Assert.That(referenced.Where(n => n.StartsWith("Microsoft.")), Is.Empty,
                $"Domain must not reference Microsoft assemblies: {string.Join(", ", referenced)}");
        });
    }

    [Test]
    public void Application_References_NeitherInfrastructureNorEfNorNpgsqlNorOptions()
    {
        var referenced = typeof(EconDataLens.Application.Cpi.ICpiIngestionRepository).Assembly
            .GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(referenced.Where(n => n == "EconDataLens.Infrastructure" || n.StartsWith("EconDataLens.Infrastructure.")),
                Is.Empty, "Application must not reference Infrastructure");
            Assert.That(referenced.Where(n => n.StartsWith("Microsoft.EntityFrameworkCore")),
                Is.Empty, "Application must not reference EF Core");
            Assert.That(referenced.Where(n => n.StartsWith("Npgsql")),
                Is.Empty, "Application must not reference Npgsql");
            Assert.That(referenced.Where(n => n.StartsWith("Microsoft.Extensions.Options")),
                Is.Empty, "Application must not reference Microsoft.Extensions.Options");
        });
    }
}
