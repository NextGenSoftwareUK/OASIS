using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;
public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task HolonCollectionsAndExportsAcknowledgeCompleteReads(bool synchronous)
    {
        var owner=Guid.NewGuid();var id=Guid.NewGuid();
        void Loaded<T>(OASISResult<T> result) {Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);Assert.IsTrue(result.IsLoaded);}
        Loaded(synchronous ? _provider.LoadAllHolons(loadChildren:false):await _provider.LoadAllHolonsAsync(loadChildren:false));
        Loaded(synchronous ? _provider.ExportAllDataForAvatarById(owner):await _provider.ExportAllDataForAvatarByIdAsync(owner));
        Assert.IsFalse((await _provider.SaveHolonAsync(new Holon {Id=id,CreatedByAvatarId=owner,Name="owned-load-flags",IsActive=true},saveChildren:false)).IsError);
        Loaded(synchronous ? _provider.LoadAllHolons(loadChildren:false):await _provider.LoadAllHolonsAsync(loadChildren:false));
        Loaded(synchronous ? _provider.ExportAll():await _provider.ExportAllAsync());
        var exported=synchronous ? _provider.ExportAllDataForAvatarById(owner):await _provider.ExportAllDataForAvatarByIdAsync(owner);
        Loaded(exported);Assert.AreEqual(id,exported.Result.Single().Id);
        var invalid=synchronous ? _provider.LoadAllHolons(maxChildDepth:-1):await _provider.LoadAllHolonsAsync(maxChildDepth:-1);
        Assert.IsTrue(invalid.IsError);Assert.IsFalse(invalid.IsLoaded);Assert.IsNull(invalid.Result);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task AvatarReadsReportLoadedOnlyAfterNativeSuccess(bool synchronous)
    {
        var id=Guid.NewGuid();var name="owned-flags-"+id.ToString("N");
        var saved=await _provider.SaveAvatarAsync(new Avatar {Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});
        Assert.IsFalse(saved.IsError,saved.Message);
        void Loaded<T>(OASISResult<T> result) {Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);Assert.IsTrue(result.IsLoaded);}
        Loaded(synchronous ? _provider.LoadAvatar(id):await _provider.LoadAvatarAsync(id));
        Loaded(synchronous ? _provider.LoadAvatarByProviderKey(id.ToString()):await _provider.LoadAvatarByProviderKeyAsync(id.ToString()));
        Loaded(synchronous ? _provider.LoadAvatarByUsername(name):await _provider.LoadAvatarByUsernameAsync(name));
        Loaded(synchronous ? _provider.LoadAvatarByEmail(name+"@example.invalid"):await _provider.LoadAvatarByEmailAsync(name+"@example.invalid"));
        Loaded(synchronous ? _provider.LoadAllAvatars():await _provider.LoadAllAvatarsAsync());
        var missing=synchronous ? _provider.LoadAvatar(Guid.NewGuid()):await _provider.LoadAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(missing.IsError);Assert.IsFalse(missing.IsLoaded);Assert.IsNull(missing.Result);
    }
}
