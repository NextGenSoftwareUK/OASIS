using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PartialManagerLoadCannotEraseRolesAndScalarOnlySavePreservesThem(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);using var other=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();(await other.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager=CosmicManager(provider,true);var reader=CosmicManager(other,true);var actor=Guid.NewGuid();
        var root=new Holon{Name="before"};root.Children.Add(new Holon{Name="first"});root.Children.Add(new Holon{Name="second"});
        (await manager.SaveHolonAsync(root,actor,new CosmicWriteOptions(Guid.NewGuid()))).IsError.Should().BeFalse();
        var loaded=await reader.LoadHolonAsync(root.Id,loadChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
        loaded.IsError.Should().BeFalse();loaded.Result.Children.Should().BeEmpty();
        var partial=(Holon)loaded.Result;var storedRoles=partial.ChildRoleManifestJson;
        HolonChildRoleManifestJson.Deserialize(storedRoles).Roles.Single().ChildIds.Count.Should().Be(2);
        var before=CosmicConcurrency.GetProviderRevisionId(partial,ProviderType.SQLLiteDBOASIS);partial.Name="scalar edit";
        var rejected=synchronous ? reader.SaveHolon(partial,actor,new CosmicWriteOptions(Guid.NewGuid()))
            :await reader.SaveHolonAsync(partial,actor,new CosmicWriteOptions(Guid.NewGuid()));
        rejected.IsError.Should().BeTrue();rejected.ErrorCode.Should().Be("HOLON_SAVE_PREPARATION_FAILED");
        rejected.Exception.Should().BeOfType<InvalidOperationException>();partial.Name.Should().Be("scalar edit");
        var unchanged=(await provider.LoadHolonAsync(root.Id)).Result;
        unchanged.Name.Should().Be("before");CosmicConcurrency.GetProviderRevisionId(unchanged,ProviderType.SQLLiteDBOASIS).Should().Be(before);
        var saved=synchronous ? reader.SaveHolon(partial,actor,new CosmicWriteOptions(Guid.NewGuid()),saveChildren:false,recursive:false)
            :await reader.SaveHolonAsync(partial,actor,new CosmicWriteOptions(Guid.NewGuid()),saveChildren:false,recursive:false);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        var current=(Holon)(await provider.LoadHolonAsync(root.Id)).Result;
        current.Name.Should().Be("scalar edit");current.Children.Select(value=>value.Name).Should().BeEquivalentTo("first","second");
        current.ChildRoleManifestJson.Should().Be(storedRoles);partial.ChildRoleManifestJson.Should().Be(storedRoles);
    }
}
