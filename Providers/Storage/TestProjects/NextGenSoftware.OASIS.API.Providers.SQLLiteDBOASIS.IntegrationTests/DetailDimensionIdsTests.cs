using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task LegacyDetailDimensionUpgradeKeepsProfileWithoutInventingRelationships(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="legacy-dimensions",XP=31,Karma=61};
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN DimensionLevelIdsJson");
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();activated.IsError.Should().BeFalse(activated.Exception?.ToString());
        var loaded=await provider.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
        loaded.Result.DimensionLevelIds.Should().BeEmpty();loaded.Result.XP.Should().Be(31);loaded.Result.Karma.Should().Be(61);
        source.DimensionLevelIds[DimensionLevel.Fifth]=Guid.NewGuid();
        var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reader.LoadAvatarDetailAsync(source.Id)).Result.DimensionLevelIds.Should().BeEquivalentTo(source.DimensionLevelIds);
    }
    [Theory][InlineData("null")][InlineData("[]")][InlineData("{\"999\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"First\":\"invalid-guid\"}")]
    [InlineData("{\"First\":\"00000000-0000-0000-0000-000000000000\",\"First\":\"00000000-0000-0000-0000-000000000000\"}")]
    [InlineData("{\"0\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task CorruptStoredDimensionMapCannotBecomeSuccessfulEmptyOrArbitraryLinks(string json)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="corrupt-dimensions"};(await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("UPDATE AvatarDetail SET DimensionLevelIdsJson={0} WHERE Id={1}",json,source.Id.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var failed=await reader.LoadAvatarDetailAsync(source.Id);failed.IsError.Should().BeTrue();failed.Result.Should().BeNull();failed.Exception.Should().NotBeNull();
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailDimensionIdentitiesSurviveNativeInsertUpdateClearAndIndependentRead(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="dimension-identities",XP=41};
        for(var revision=0;revision<3;revision++){
            source.DimensionLevelIds=revision==2?new Dictionary<DimensionLevel,Guid>():new(){{DimensionLevel.First,Guid.NewGuid()},{DimensionLevel.Twelfth,Guid.NewGuid()}};
            var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(source.Id):await reader.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            loaded.Result.DimensionLevelIds.Should().BeEquivalentTo(source.DimensionLevelIds);loaded.Result.XP.Should().Be(41);
            using var control=new DataContext(TestConnectionString);
            using var connection=control.Database.GetDbConnection();connection.Open();using var command=connection.CreateCommand();
            command.CommandText="SELECT DimensionLevelIdsJson FROM AvatarDetail WHERE Id=$id";
            var parameter=command.CreateParameter();parameter.ParameterName="$id";parameter.Value=source.Id.ToString();command.Parameters.Add(parameter);
            using var native=System.Text.Json.JsonDocument.Parse((string)command.ExecuteScalar()!);
            native.RootElement.EnumerateObject().Count().Should().Be(source.DimensionLevelIds.Count);
            foreach(var pair in source.DimensionLevelIds)Guid.Parse(native.RootElement.GetProperty(pair.Key.ToString()).GetString()!).Should().Be(pair.Value);
        }
    }
}
