using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false,false)] [InlineData(true,false,false)]
    [InlineData(false,true,false)] [InlineData(true,true,false)]
    [InlineData(false,false,true)] [InlineData(true,false,true)]
    [InlineData(false,true,true)] [InlineData(true,true,true)]
    public async Task V2EncryptedGraphLoadsDecryptAllSelectedMembersOrReturnNoGraph(bool synchronous, bool invalidChild, bool point)
    {
        var settings = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('e',32) };
        var rootCipher = PasswordEncryptionHelper.EncryptValue("{\"root-note\":\"root private\"}", settings);
        var childCipher = invalidChild ? "{\"child-note\":\"plaintext\"}" : PasswordEncryptionHelper.EncryptValue("{\"child-note\":\"child private\"}", settings);
        var root = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["__oasis_enc__"] = rootCipher } };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, MetaData = new() { ["__oasis_enc__"] = childCipher } };
        root.Children.Add(child);
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var saved = await provider.SaveHolonAsync(root, saveChildren:true);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var beforeRoot = StoredRecordJson.Serialize((await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result);
        var beforeChild = StoredRecordJson.Serialize((await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result);
        var manager = CosmicManager(provider,true);
        manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = settings };
        void AssertFailure<T>(OASISResult<T> result)
        {
            result.IsError.Should().BeTrue(); result.Result.Should().BeNull();
            result.ErrorCode.Should().Be("HOLON_METADATA_DECRYPTION_FAILED");
        }
        IHolon loaded = null!;
        if (point)
        {
            var result = synchronous ? manager.LoadHolon(root.Id,maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS)
                : await manager.LoadHolonAsync(root.Id,maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS);
            if (invalidChild) AssertFailure(result);
            else { result.IsError.Should().BeFalse(result.Message + result.Exception); loaded = result.Result; }
        }
        else
        {
            var result = synchronous ? manager.LoadAllHolons(maxChildDepth:1,cache:false,providerType:ProviderType.SQLLiteDBOASIS)
                : await manager.LoadAllHolonsAsync(maxChildDepth:1,providerType:ProviderType.SQLLiteDBOASIS);
            if (invalidChild) AssertFailure(result);
            else { result.IsError.Should().BeFalse(result.Message + result.Exception); loaded = result.Result.Single(row => row.Id == root.Id); }
        }
        if (!invalidChild)
        {
            loaded.MetaData["root-note"].ToString().Should().Be("root private");
            var member = loaded.Children.Single(); member.Id.Should().Be(child.Id);
            member.MetaData["child-note"].ToString().Should().Be("child private");
            loaded.MetaData.Should().NotContainKey("__oasis_enc__"); member.MetaData.Should().NotContainKey("__oasis_enc__");
            HolonChangeTracking.HasChanges(loaded).Should().BeFalse(); HolonChangeTracking.HasChanges(member).Should().BeFalse();
        }
        StoredRecordJson.Serialize((await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result).Should().Be(beforeRoot);
        StoredRecordJson.Serialize((await provider.LoadHolonAsync(child.Id,loadChildren:false)).Result).Should().Be(beforeChild);
    }
}
