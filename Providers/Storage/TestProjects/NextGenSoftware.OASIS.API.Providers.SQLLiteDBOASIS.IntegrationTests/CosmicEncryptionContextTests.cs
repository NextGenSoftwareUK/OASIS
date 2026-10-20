using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task V2NativeSaveUsesIndependentRootAndChildEncryptionContexts(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager=CosmicManager(provider,true);
        manager.OASISDNA.OASIS.Security=new SecuritySettings { HolonDataEncryption=new EncryptionSettings {
            Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('g',32) } };
        var child=new Holon { Name="encrypted-child",DataEncryptionOverride=new EncryptionSettings {
            Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('c',32) } };
        var root=new Holon { Name="encrypted-root" };
        root.Children.Add(child);
        root.MetaData["root-secret"]="root-value";child.MetaData["child-secret"]="child-value";
        var saved=synchronous ? manager.SaveHolon(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS)
            :await manager.SaveHolonAsync(root,Guid.NewGuid(),providerType:ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message+saved.Exception);
        root.MetaData["root-secret"].ToString().Should().Be("root-value");
        child.MetaData["child-secret"].ToString().Should().Be("child-value");
        root.HasHolonChanged().Should().BeFalse();
        var storedRoot=await provider.LoadHolonAsync(root.Id,loadChildren:false);
        var storedChild=await provider.LoadHolonAsync(child.Id,loadChildren:false);
        storedRoot.IsError.Should().BeFalse(storedRoot.Message+storedRoot.Exception);
        storedChild.IsError.Should().BeFalse(storedChild.Message+storedChild.Exception);
        storedRoot.Result.MetaData.Should().ContainKey("__oasis_enc__").And.NotContainKey("root-secret");
        storedChild.Result.MetaData.Should().ContainKey("__oasis_enc__").And.NotContainKey("child-secret");
        storedRoot.Result.MetaData["__oasis_enc__"].ToString().Should().NotContain("root-value");
        storedChild.Result.MetaData["__oasis_enc__"].ToString().Should().NotContain("child-value");
    }
}
