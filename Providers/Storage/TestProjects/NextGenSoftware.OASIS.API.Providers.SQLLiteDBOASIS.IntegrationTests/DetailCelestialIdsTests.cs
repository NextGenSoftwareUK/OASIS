using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LegacyDetailCelestialUpgradeKeepsBasicParentAndProfileWithoutInventingLinks(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="legacy-celestial",ParentHolonId=Guid.NewGuid(),XP=47,Karma=71};
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN CelestialParentIdsJson");
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();activated.IsError.Should().BeFalse(activated.Exception?.ToString());
        var loaded=await provider.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
        loaded.Result.ParentStarId.Should().Be(Guid.Empty);loaded.Result.ParentHolonId.Should().Be(source.ParentHolonId);loaded.Result.XP.Should().Be(47);loaded.Result.Karma.Should().Be(71);
        source.ParentStarId=Guid.NewGuid();source.DimensionLevel=DimensionLevel.Ninth;
        var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var restored=(await reader.LoadAvatarDetailAsync(source.Id)).Result;restored.ParentStarId.Should().Be(source.ParentStarId);restored.DimensionLevel.Should().Be(DimensionLevel.Ninth);
    }
    [Theory][InlineData("null")][InlineData("array")][InlineData("missing")][InlineData("unknown")][InlineData("duplicate")][InlineData("guid")][InlineData("level")]
    public async Task CorruptCelestialSnapshotCannotReturnPartialOrInventedRelationships(string mode)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="corrupt-celestial"};(await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        var node=System.Text.Json.JsonSerializer.SerializeToNode(HolonCelestialParentIds.Capture(source))!.AsObject();
        if(mode=="missing")node.Remove("ParentStarId");if(mode=="unknown")node["ParentStar"]="must-not-embed-navigation";
        if(mode=="guid")node["ParentStarId"]="not-a-guid";if(mode=="level")node["DimensionLevel"]=999;
        var json=mode=="null"?"null":mode=="array"?"[]":mode=="duplicate"?node.ToJsonString().Insert(1,"\"ParentStarId\":\"00000000-0000-0000-0000-000000000000\","):node.ToJsonString();
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("UPDATE AvatarDetail SET CelestialParentIdsJson={0} WHERE Id={1}",json,source.Id.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var failed=await reader.LoadAvatarDetailAsync(source.Id);failed.IsError.Should().BeTrue();failed.Result.Should().BeNull();failed.Exception.Should().NotBeNull();
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailCelestialParentIdsSurviveNativeInsertUpdateClearAndIndependentRead(bool synchronous)
    {
        var parents=typeof(IHolon).GetProperties().Where(property=>property.PropertyType==typeof(Guid)&&property.Name.StartsWith("Parent",StringComparison.Ordinal)).ToArray();
        parents.Length.Should().Be(16,"all declared celestial parent identities must be covered");
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="celestial-parent-identities",XP=37};
        for(var revision=0;revision<3;revision++){
            foreach(var property in parents)property.SetValue(source,revision==2?Guid.Empty:Guid.NewGuid());
            source.DimensionLevel=revision==2?DimensionLevel.First:DimensionLevel.Twelfth;
            source.SubDimensionLevel=revision==2?SubDimensionLevel.None:SubDimensionLevel.Fifth;
            var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(source.Id):await reader.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            foreach(var property in parents)property.GetValue(loaded.Result).Should().Be(property.GetValue(source),property.Name);
            loaded.Result.DimensionLevel.Should().Be(source.DimensionLevel);loaded.Result.SubDimensionLevel.Should().Be(source.SubDimensionLevel);loaded.Result.XP.Should().Be(37);
            using var control=new DataContext(TestConnectionString);
            var raw=control.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.AvatarDetailModel>().AsNoTracking().Single(row=>row.Id==source.Id.ToString());
            using var native=System.Text.Json.JsonDocument.Parse(raw.CelestialParentIdsJson);
            foreach(var property in parents)native.RootElement.GetProperty(property.Name).GetGuid().Should().Be((Guid)property.GetValue(source)!);
        }
    }
}
