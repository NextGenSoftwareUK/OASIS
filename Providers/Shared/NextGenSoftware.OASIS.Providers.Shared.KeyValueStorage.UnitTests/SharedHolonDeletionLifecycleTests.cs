using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class SharedHolonDeletionLifecycleTests
{
    private sealed class Provider(IKeyValueBackend backend):KeyValueStorageProviderBase(backend)
    {
        public Provider():this(new InMemoryBackend()){}
        public Task<OASISResult<IHolon>> Soft(Guid id)=>DeleteHolonRecordAsync(id,true);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task SharedSoftDeletePreservesHistoryAndHardDeleteRemovesIt(bool synchronous)
    {
        var provider=new Provider{IsVersionControlEnabled=true,ProviderName="controlled lifecycle",ProviderType=new EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.RedisOASIS)};
        var source=new Holon{Id=Guid.NewGuid(),IsActive=true,Description="original"};
        var saved=await provider.SaveHolonAsync(source,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
        var deleted=synchronous?provider.Soft(source.Id).GetAwaiter().GetResult():await provider.Soft(source.Id);
        Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.IsDeleted);Assert.AreEqual(1,deleted.DeletedCount);
        Assert.IsTrue(deleted.Result.IsDeleted);Assert.IsFalse(deleted.Result.IsActive);Assert.AreEqual(2,deleted.Result.Version);
        Assert.AreEqual(1,(await provider.LoadHolonAsync(source.Id,version:1)).Result.Version);
        var repeat=await provider.Soft(source.Id);Assert.IsFalse(repeat.IsError,repeat.Message);Assert.AreEqual(0,repeat.DeletedCount);
        var hard=synchronous?provider.DeleteHolon(source.Id):await provider.DeleteHolonAsync(source.Id);
        Assert.IsFalse(hard.IsError,hard.Message);Assert.AreEqual(1,hard.DeletedCount);
        Assert.IsNull((await provider.LoadHolonAsync(source.Id,version:1)).Result);
    }
}
