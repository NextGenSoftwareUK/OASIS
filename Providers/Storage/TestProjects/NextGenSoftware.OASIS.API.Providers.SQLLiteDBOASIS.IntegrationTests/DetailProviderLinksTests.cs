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
    public async Task LegacyDetailProviderLinksUpgradePreservesProfileAndDoesNotInventKeys(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="legacy-links",XP=53,Karma=73};
        (await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))control.Database.ExecuteSqlRaw("ALTER TABLE AvatarDetail DROP COLUMN ProviderLinksJson");
        var activated=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();activated.IsError.Should().BeFalse(activated.Exception?.ToString());
        var loaded=await provider.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
        loaded.Result.ProviderUniqueStorageKey.Should().BeEmpty();loaded.Result.PreviousVersionProviderUniqueStorageKey.Should().BeEmpty();loaded.Result.ProviderMetaData.Should().BeEmpty();
        loaded.Result.XP.Should().Be(53);loaded.Result.Karma.Should().Be(73);loaded.Result.Username.Should().Be(source.Username);
        source.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]="post-upgrade";
        var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await reader.LoadAvatarDetailAsync(source.Id)).Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be("post-upgrade");
    }
    [Theory][InlineData("null")][InlineData("[]")][InlineData("{\"Current\":null}")][InlineData("{\"Metadata\":{\"999\":{}}}")]
    public async Task InvalidStoredDetailProviderLinksFailInsteadOfReturningEmptyKeys(string json)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="corrupt-links"};(await provider.SaveAvatarDetailAsync(source)).IsError.Should().BeFalse();
        using(var control=new DataContext(TestConnectionString))
            control.Database.ExecuteSqlRaw("UPDATE AvatarDetail SET ProviderLinksJson={0} WHERE Id={1}",json,source.Id.ToString());
        using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
        var failed=await reader.LoadAvatarDetailAsync(source.Id);failed.IsError.Should().BeTrue();failed.Result.Should().BeNull();failed.Exception.Should().NotBeNull();
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task DetailProviderLinksSurviveNativeInsertUpdateAndIndependentRead(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);(await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new AvatarDetail{Id=Guid.NewGuid(),Username="provider-links",XP=39};
        for(var revision=1;revision<=2;revision++){
            source.ProviderUniqueStorageKey=new(){{ProviderType.MongoDBOASIS,"current-"+revision}};
            source.PreviousVersionProviderUniqueStorageKey=new(){{ProviderType.MongoDBOASIS,"previous-"+revision}};
            source.ProviderMetaData=new(){{ProviderType.MongoDBOASIS,new(){{"partition","native-"+revision},{"unicode","🌳"}}}};
            var saved=synchronous?provider.SaveAvatarDetail(source):await provider.SaveAvatarDetailAsync(source);saved.IsError.Should().BeFalse(saved.Exception?.ToString());
            using var reader=new SQLLiteDBOASIS(TestConnectionString);(await reader.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=synchronous?reader.LoadAvatarDetail(source.Id):await reader.LoadAvatarDetailAsync(source.Id);loaded.IsError.Should().BeFalse(loaded.Exception?.ToString());
            loaded.Result.ProviderUniqueStorageKey.Should().BeEquivalentTo(source.ProviderUniqueStorageKey);
            loaded.Result.PreviousVersionProviderUniqueStorageKey.Should().BeEquivalentTo(source.PreviousVersionProviderUniqueStorageKey);
            loaded.Result.ProviderMetaData.Should().BeEquivalentTo(source.ProviderMetaData);loaded.Result.XP.Should().Be(39);
            using var control=new DataContext(TestConnectionString);
            var raw=control.Set<NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities.AvatarDetailModel>().AsNoTracking().Single(row=>row.Id==source.Id.ToString());
            using var native=System.Text.Json.JsonDocument.Parse(raw.ProviderLinksJson);
            native.RootElement.GetProperty("Current").GetProperty("MongoDBOASIS").GetString().Should().Be("current-"+revision);
            native.RootElement.GetProperty("PreviousVersion").GetProperty("MongoDBOASIS").GetString().Should().Be("previous-"+revision);
            native.RootElement.GetProperty("Metadata").GetProperty("MongoDBOASIS").GetProperty("unicode").GetString().Should().Be("🌳");
        }
    }
}
