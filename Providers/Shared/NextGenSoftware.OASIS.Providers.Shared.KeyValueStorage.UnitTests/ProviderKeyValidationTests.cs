using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;
[TestClass]
public class ProviderKeyValidationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidGuidKeysFailTerminallyWithoutStorageReads(bool synchronous)
    {
        var backend=new InMemoryBackend {ThrowReads=true};var provider=new TestProvider(backend);
        void Invalid<T>(OASISResult<T> result)
        {Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));}
        foreach(var key in new[] {null," ","not-a-guid",Guid.Empty.ToString()})
        {
            Invalid(synchronous ? provider.LoadAvatarByProviderKey(key):await provider.LoadAvatarByProviderKeyAsync(key));
            Invalid(synchronous ? provider.DeleteAvatar(key):await provider.DeleteAvatarAsync(key));
            Invalid(synchronous ? provider.LoadHolon(key,loadChildren:false):await provider.LoadHolonAsync(key,loadChildren:false));
            Invalid(synchronous ? provider.LoadHolonsForParent(key,loadChildren:false):await provider.LoadHolonsForParentAsync(key,loadChildren:false));
            Invalid(synchronous ? provider.DeleteHolon(key):await provider.DeleteHolonAsync(key));
        }
        Assert.AreEqual(0,backend.ListCalls);
    }
}
