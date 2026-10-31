using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task V2PendingValuePreviewTracksNativeLoadAndAcknowledgedSave(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager=CosmicManager(provider,true);var actor=Guid.NewGuid();
        var source=new Holon{Name="preview-original"};source.MetaData["editable"]="before";
        var created=await manager.SaveHolonAsync(source,actor,providerType:ProviderType.SQLLiteDBOASIS);
        created.IsError.Should().BeFalse(created.Message+created.Exception);
        var loaded=synchronous ? manager.LoadHolon(source.Id,providerType:ProviderType.SQLLiteDBOASIS)
            :await manager.LoadHolonAsync(source.Id,providerType:ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse(loaded.Message+loaded.Exception);
        var value=loaded.Result;
        HolonChangeTracking.PreviewValueChanges(value).Result.Should().BeEmpty();
        value.MetaData["editable"]="after";
        var pending=HolonChangeTracking.PreviewValueChanges(value);
        pending.IsError.Should().BeFalse();
        var delta=pending.Result.Single(change=>change.Path=="/MetaData/editable");
        delta.PreviousJson.Should().Be("\"before\"");delta.CurrentJson.Should().Be("\"after\"");
        var saved=synchronous ? manager.SaveHolon(value,actor,providerType:ProviderType.SQLLiteDBOASIS)
            :await manager.SaveHolonAsync(value,actor,providerType:ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        HolonChangeTracking.PreviewValueChanges(value).Result.Should().BeEmpty();
        (await provider.LoadHolonAsync(value.Id)).Result.MetaData["editable"].ToString().Should().Be("after");
        delta.PreviousJson.Should().Be("\"before\"");delta.CurrentJson.Should().Be("\"after\"");
    }
}
