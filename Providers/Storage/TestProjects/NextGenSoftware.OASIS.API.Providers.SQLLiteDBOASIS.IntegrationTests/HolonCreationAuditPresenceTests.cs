using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task HolonCreationAuditPresencePreservesAbsentVersusExplicitEnumsAndImmutableOrigin(bool providerPresent,bool oasisPresent)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new Holon{Id=Guid.NewGuid(),Name="owned nullable audit",
            CreatedProviderType=providerPresent ? new NextGenSoftware.Utilities.EnumValue<ProviderType>((ProviderType)0):null,
            CreatedOASISType=oasisPresent ? new NextGenSoftware.Utilities.EnumValue<OASISType>((OASISType)0):null};
        var stored=await provider.SaveHolonAsync(source,saveChildren:false);stored.IsError.Should().BeFalse(stored.Message+stored.Exception);
        (stored.Result.CreatedProviderType!=null).Should().Be(providerPresent);(stored.Result.CreatedOASISType!=null).Should().Be(oasisPresent);
        using(var control=new DataContext(TestConnectionString))
        {
            var row=control.Holons.AsNoTracking().Single();row.HasCreatedProviderType.Should().Be(providerPresent);row.HasCreatedOASISType.Should().Be(oasisPresent);
        }
        var changed=StoredRecordJson.Project<Holon>(stored.Result);changed.Name="owned update";
        changed.CreatedProviderType=providerPresent ? null:new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.Neo4jOASIS);
        changed.CreatedOASISType=oasisPresent ? null:new NextGenSoftware.Utilities.EnumValue<OASISType>((OASISType)0);
        var update=await provider.SaveHolonAsync(changed,saveChildren:false);update.IsError.Should().BeFalse(update.Message+update.Exception);
        (update.Result.CreatedProviderType!=null).Should().Be(providerPresent);(update.Result.CreatedOASISType!=null).Should().Be(oasisPresent);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HolonCreationAuditPresenceMigrationRefusesToInventAbsentOrigin(bool absent)
    {
        const string previous="20261019220000_AddCosmicReplicaHeads";
        const string current="20261019230000_AddHolonCreationAuditPresence";
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid(),Name="owned audit migration",
                CreatedProviderType=absent ? null:new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.Neo4jOASIS),
                CreatedOASISType=new NextGenSoftware.Utilities.EnumValue<OASISType>((OASISType)0)},saveChildren:false)).IsError.Should().BeFalse();
        }
        using var control=new DataContext(TestConnectionString);var migrator=control.GetService<IMigrator>();
        if(absent)
        {
            Func<Task> down=()=>migrator.MigrateAsync(previous);await down.Should().ThrowAsync<SqliteException>();
            (await control.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
            control.Holons.AsNoTracking().Single().HasCreatedProviderType.Should().BeFalse();
        }
        else
        {
            await migrator.MigrateAsync(previous);(await control.Database.GetAppliedMigrationsAsync()).Should().NotContain(current);
            await migrator.MigrateAsync();control.Holons.AsNoTracking().Single().HasCreatedProviderType.Should().BeTrue();
        }
    }

    [Fact]
    public async Task HolonCreationAuditPresenceUpgradeRetainsLegacyObservableEnumValues()
    {
        var id=Guid.NewGuid();
        using(var original=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await original.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await original.SaveHolonAsync(new Holon{Id=id,Name="owned old audit columns",
                CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.Neo4jOASIS),
                CreatedOASISType=new NextGenSoftware.Utilities.EnumValue<OASISType>((OASISType)0)},saveChildren:false)).IsError.Should().BeFalse();
        }
        using(var legacy=new DataContext(TestConnectionString))
        {
            legacy.Database.ExecuteSqlRaw("ALTER TABLE Holon DROP COLUMN HasCreatedProviderType");
            legacy.Database.ExecuteSqlRaw("ALTER TABLE Holon DROP COLUMN HasCreatedOASISType");
        }
        using var upgraded=new SQLLiteDBOASIS(TestConnectionString);(await upgraded.ActivateProviderAsync()).IsError.Should().BeFalse();
        var read=await upgraded.LoadHolonAsync(id,loadChildren:false);read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.CreatedProviderType.Value.Should().Be(ProviderType.Neo4jOASIS);read.Result.CreatedOASISType.Should().NotBeNull();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HolonMetadataValuesRemainCanonicalAcrossClrAndJsonTokenRepresentations(bool jsonToken)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row=new Holon{Id=Guid.NewGuid(),Name="owned metadata codec"};
        row.MetaData["nested"]=jsonToken ? JObject.Parse("{\"exact\":9007199254740993,\"labels\":[\"a\",\"b\"]}")
            :new Dictionary<string,object>{{"exact",9007199254740993L},{"labels",new[]{"a","b"}}};
        var expected=JToken.Parse(StoredRecordJson.Serialize(row.MetaData["nested"]));
        var saved=await provider.SaveHolonAsync(row,saveChildren:false);saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        JToken.DeepEquals(expected,JToken.Parse(StoredRecordJson.Serialize(saved.Result.MetaData["nested"]))).Should().BeTrue();
        var read=await provider.LoadHolonAsync(row.Id,loadChildren:false);read.IsError.Should().BeFalse(read.Message+read.Exception);
        JToken.DeepEquals(expected,JToken.Parse(StoredRecordJson.Serialize(read.Result.MetaData["nested"]))).Should().BeTrue();
    }
}
