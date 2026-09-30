using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pino.Data;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Data;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
public sealed class ApplicationDbContextTests
{
    [Fact]
    public void ApplicationEntitiesUseNamedSetsAndConventionalTableNames()
    {
        using var services = CreateServices();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var sets = typeof(ApplicationDbContext)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(property => property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToDictionary(property => property.PropertyType.GenericTypeArguments[0], property => property.Name);
        var entities = db.Model.GetEntityTypes()
            .Where(entity => entity.ClrType.Assembly == typeof(ApplicationDbContext).Assembly && entity.ClrType != typeof(ApplicationUser))
            .ToArray();

        entities.ShouldNotBeEmpty();
        sets.Keys.Select(type => type.FullName).Order(StringComparer.Ordinal)
            .ShouldBe(entities.Select(entity => entity.ClrType.FullName).Order(StringComparer.Ordinal));
        foreach (var entity in entities)
        {
            entity.GetTableName().ShouldBe(sets[entity.ClrType]);
        }
    }

    [Fact]
    public void IdentityKeepsItsDefaultUserAndVersionThreePasskeyTables()
    {
        using var services = CreateServices();
        var db = services.GetRequiredService<ApplicationDbContext>();

        db.Model.FindEntityType(typeof(ApplicationUser)).ShouldNotBeNull().GetTableName().ShouldBe("AspNetUsers");
        db.Model.FindEntityType(typeof(IdentityUserPasskey<string>)).ShouldNotBeNull().GetTableName().ShouldBe("AspNetUserPasskeys");
    }

    [Fact]
    public void MigrationSnapshotMatchesTheApplicationModel()
    {
        using var services = CreateServices();
        var db = services.GetRequiredService<ApplicationDbContext>();

        db.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Model inspection never opens a connection. Use the production provider and Identity schema options.
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql("Host=localhost;Database=pino_model_tests"));
        services.AddIdentityCore<ApplicationUser>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }
}
