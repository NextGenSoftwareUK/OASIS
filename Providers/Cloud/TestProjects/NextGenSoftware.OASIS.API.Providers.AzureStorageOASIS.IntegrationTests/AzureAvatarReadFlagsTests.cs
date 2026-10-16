using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;
public partial class AzureStorageOASISIntegrationTests
{
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
