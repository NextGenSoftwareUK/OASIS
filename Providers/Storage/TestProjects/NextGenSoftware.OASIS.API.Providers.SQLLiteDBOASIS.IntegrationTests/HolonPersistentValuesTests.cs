using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static Holon PersistentValueFixture()=>new(){Id=Guid.NewGuid(),Name="owned extension root",CustomKey="owned-custom-key",
        ParentHolonId2=Guid.NewGuid(),ParentHolonId3=Guid.NewGuid(),ParentHolonIds=new List<Guid>{Guid.NewGuid(),Guid.NewGuid()},
        ChildrenIds=new List<Guid>{Guid.NewGuid()},ChildIdListCache="owned-direct-cache",AllChildIdListCache="owned-all-cache",
        PreviousVersionProviderUniqueStorageKey=new Dictionary<ProviderType,string>{{ProviderType.Neo4jOASIS,"owned-previous-key"}},
        ProviderMetaData=new Dictionary<ProviderType,Dictionary<string,string>>{
            [ProviderType.Neo4jOASIS]=new(){{"origin-note","retained"}},
            [ProviderType.SQLLiteDBOASIS]=new(){{"local-note","retained"},{CosmicConcurrency.ProviderRevisionMetadataKey,Guid.NewGuid().ToString()}}},
        MetaData=new Dictionary<string,object>{{"exact",9007199254740993L},{"nested",JsonDocument.Parse("{\"labels\":[\"one\",\"two\"],\"flag\":true}").RootElement.Clone()}}};
    private static void AssertPersistentValues(IHolon expected,IHolon actual)
    {
        ((Holon)actual).CustomKey.Should().Be(((Holon)expected).CustomKey);actual.ParentHolonId2.Should().Be(expected.ParentHolonId2);
        actual.ParentHolonId3.Should().Be(expected.ParentHolonId3);actual.ParentHolonIds.Should().Equal(expected.ParentHolonIds);
        ((Holon)actual).ChildrenIds.Should().Equal(((Holon)expected).ChildrenIds);actual.ChildIdListCache.Should().Be(expected.ChildIdListCache);
        actual.AllChildIdListCache.Should().Be(expected.AllChildIdListCache);
        actual.PreviousVersionProviderUniqueStorageKey.Should().BeEquivalentTo(expected.PreviousVersionProviderUniqueStorageKey);
        actual.ProviderMetaData[ProviderType.Neo4jOASIS].Should().BeEquivalentTo(expected.ProviderMetaData[ProviderType.Neo4jOASIS]);
        actual.ProviderMetaData[ProviderType.SQLLiteDBOASIS]["local-note"].Should().Be("retained");
        actual.MetaData["exact"].ToString().Should().Be("9007199254740993");
        JToken.DeepEquals(JToken.Parse(StoredRecordJson.Serialize(expected.MetaData["nested"])),
            JToken.Parse(StoredRecordJson.Serialize(actual.MetaData["nested"]))).Should().BeTrue();
        actual.IsNewHolon.Should().BeFalse();
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task HolonPersistentValuesRoundTripWithoutNavigationOrDuplicateColumnOwnership(bool synchronous,bool batch)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=PersistentValueFixture();var unselected=new Holon{Id=Guid.NewGuid(),Name="must not persist"};
        root.ParentHolon=root;root.Children=new List<IHolon>{unselected};
        IHolon acknowledged;
        if(batch)
        {
            var result=synchronous ? provider.SaveHolons(new[]{root},saveChildren:false):await provider.SaveHolonsAsync(new[]{root},saveChildren:false);
            result.IsError.Should().BeFalse(result.Message+result.Exception);acknowledged=result.Result.Single();
        }
        else
        {
            var result=synchronous ? provider.SaveHolon(root,saveChildren:false):await provider.SaveHolonAsync(root,saveChildren:false);
            result.IsError.Should().BeFalse(result.Message+result.Exception);acknowledged=result.Result;
        }
        AssertPersistentValues(root,acknowledged);
        var read=synchronous ? provider.LoadHolon(root.Id):await provider.LoadHolonAsync(root.Id);
        read.IsError.Should().BeFalse(read.Message+read.Exception);AssertPersistentValues(root,read.Result);read.Result.Children.Should().BeEmpty();
        using var control=new DataContext(TestConnectionString);var row=control.Holons.AsNoTracking().Single();
        var extension=JObject.Parse(row.PersistentValueJson);
        foreach(var field in new[]{"Id","Name","VersionId","MetaData","ProviderUniqueStorageKey","Children","ParentHolon","IsNewHolon"})extension.Property(field).Should().BeNull();
        extension["ProviderMetaData"]![ProviderType.SQLLiteDBOASIS.ToString()]![CosmicConcurrency.ProviderRevisionMetadataKey].Should().BeNull();
        CosmicConcurrency.GetProviderRevisionId(read.Result,ProviderType.SQLLiteDBOASIS).Should().Be(row.ProviderRevisionId);
        read.Result.InstanceSavedOnProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        var revision=row.ProviderRevisionId;
        ((Holon)read.Result).CustomKey="owned changed key";read.Result.ProviderMetaData[ProviderType.Neo4jOASIS]["origin-note"]="changed";
        var changed=await provider.SaveHolonAsync(read.Result,saveChildren:false);changed.IsError.Should().BeFalse(changed.Message+changed.Exception);
        var reloaded=await provider.LoadHolonAsync(root.Id);reloaded.IsError.Should().BeFalse(reloaded.Message+reloaded.Exception);
        ((Holon)reloaded.Result).CustomKey.Should().Be("owned changed key");reloaded.Result.ProviderMetaData[ProviderType.Neo4jOASIS]["origin-note"].Should().Be("changed");
        CosmicConcurrency.GetProviderRevisionId(reloaded.Result,ProviderType.SQLLiteDBOASIS).Should().NotBe(revision);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HolonPersistentValuesAreBoundIntoConditionalReceiptAndReplay(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=PersistentValueFixture();var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,
            new[]{new HolonWriteCondition(source.Id,null)}),ConditionalHolonOperation.Save,new[]{source},saveChildren:false);
        var result=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        result.IsError.Should().BeFalse(result.Message+result.Exception);AssertPersistentValues(source,result.Result.Single());
        var changed=StoredRecordJson.Project<Holon>(result.Result.Single());changed.CustomKey="later state";
        (await provider.SaveHolonAsync(changed,saveChildren:false)).IsError.Should().BeFalse();
        var replay=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        replay.IsError.Should().BeFalse(replay.Message+replay.Exception);AssertPersistentValues(source,replay.Result.Single());
        replay.Result.Single().VersionId.Should().Be(result.Result.Single().VersionId);
        ((Holon)(await provider.LoadHolonAsync(source.Id)).Result).CustomKey.Should().Be("later state");
        source.CustomKey="reused operation with changed extension";
        provider.ExecuteConditionalHolonWrite(request).ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED");
    }

    [Theory]
    [InlineData("invalid-json")] [InlineData("array")] [InlineData("null")] [InlineData("column")]
    [InlineData("unknown")] [InlineData("navigation")] [InlineData("revision")] [InlineData("root-type")]
    public async Task HolonPersistentValuesRejectCorruptionInsteadOfReturningPartialOrSpoofedData(string mode)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=PersistentValueFixture();(await provider.SaveHolonAsync(root,saveChildren:false)).IsError.Should().BeFalse();
        var values=mode switch{
            "invalid-json"=>"{","array"=>"[]","null"=>"null","column"=>"{\"Name\":\"spoofed\"}",
            "unknown"=>"{\"FutureUnknownField\":\"must not disappear\"}","navigation"=>"{\"Children\":[]}",
            "revision"=>"{\"ProviderMetaData\":{\"SQLLiteDBOASIS\":{\"COSMIC.ProviderRevisionId\":\""+Guid.NewGuid()+"\"}}}",
            _=>"{\"$type\":\"NextGenSoftware.OASIS.API.Core.Holons.Holon, NextGenSoftware.OASIS.API.Core\"}"};
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("UPDATE Holon SET PersistentValueJson={0} WHERE Id={1}",values,root.Id.ToString());
        var rejected=await provider.LoadHolonAsync(root.Id);rejected.IsError.Should().BeTrue();rejected.Exception.Should().NotBeNull();
        rejected.Result.Should().BeNull();
    }

    [Fact]
    public async Task HolonPersistentValuesUpgradeDoesNotInventHistoricalValuesOrRotateExistingToken()
    {
        var id=Guid.NewGuid();var logical=Guid.NewGuid();Guid? token;
        using(var original=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await original.ActivateProviderAsync()).IsError.Should().BeFalse();
            var stored=await original.SaveHolonAsync(new Holon{Id=id,Name="owned pre-extension row",Version=4,VersionId=logical},saveChildren:false);
            stored.IsError.Should().BeFalse(stored.Message);token=CosmicConcurrency.GetProviderRevisionId(stored.Result,ProviderType.SQLLiteDBOASIS);
        }
        using(var legacy=new DataContext(TestConnectionString))legacy.Database.ExecuteSqlRaw("ALTER TABLE Holon DROP COLUMN PersistentValueJson");
        using var upgraded=new SQLLiteDBOASIS(TestConnectionString);(await upgraded.ActivateProviderAsync()).IsError.Should().BeFalse();
        var read=await upgraded.LoadHolonAsync(id);read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.Name.Should().Be("owned pre-extension row");read.Result.VersionId.Should().Be(logical);read.Result.Version.Should().Be(4);
        ((Holon)read.Result).CustomKey.Should().BeNull();read.Result.ParentHolonId2.Should().BeEmpty();
        CosmicConcurrency.GetProviderRevisionId(read.Result,ProviderType.SQLLiteDBOASIS).Should().Be(token);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HolonPersistentValuesEfMigrationMatchesModelAndRefusesLossyDowngrade(bool hasValues)
    {
        const string previous="20261019191942_AddHolonProviderRevision";
        const string current="20261019210000_AddHolonPersistentValues";
        using(var initial=new DataContext(TestConnectionString))await initial.Database.MigrateAsync();
        Guid id=Guid.Empty;
        if(hasValues)
        {
            using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var row=PersistentValueFixture();id=row.Id;(await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
        }
        using var control=new DataContext(TestConnectionString);var migrator=control.GetService<IMigrator>();
        if(hasValues)
        {
            Func<Task> down=()=>migrator.MigrateAsync(previous);await down.Should().ThrowAsync<SqliteException>();
            (await control.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
            control.Holons.AsNoTracking().Single(row=>row.Id==id.ToString()).PersistentValueJson.Should().Contain("owned-custom-key");
            await control.Database.OpenConnectionAsync();
            try
            {
                using var command=control.Database.GetDbConnection().CreateCommand();
                command.CommandText="SELECT COUNT(*) FROM __EFMigrationsLock";
                Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0,"rejected downgrade must release the native migration lock");
            }
            finally{await control.Database.CloseConnectionAsync();}
        }
        else
        {
            await migrator.MigrateAsync(previous);(await control.Database.GetAppliedMigrationsAsync()).Should().NotContain(current);
            await migrator.MigrateAsync();(await control.Database.GetAppliedMigrationsAsync()).Should().Contain(current);
            control.Holons.AsNoTracking().Should().BeEmpty();
        }
    }
}
