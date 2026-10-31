using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class SharedPointIdentityAdmissionTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    [DataRow(3,false)][DataRow(3,true)][DataRow(4,false)][DataRow(4,true)][DataRow(5,false)][DataRow(5,true)]
    public async Task EmptyTypedPointIdentityRejectsBeforeAnyBackendOperation(int operation,bool sync)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        void Check<T>(OASISResult<T> result)
        {
            Assert.AreEqual(0,backend.GetCalls,"Invalid typed identity must fail before a potentially billable read.");
            Assert.AreEqual(0,backend.PutCalls);Assert.AreEqual(0,backend.DeleteCalls);Assert.AreEqual(0,backend.ListCalls);
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            Assert.IsFalse(result.IsLoaded);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.LoadedCount);Assert.AreEqual(0,result.DeletedCount);
        }
        switch(operation){
            case 0:Check(sync?provider.LoadAvatar(Guid.Empty):await provider.LoadAvatarAsync(Guid.Empty));break;
            case 1:Check(sync?provider.LoadAvatarDetail(Guid.Empty):await provider.LoadAvatarDetailAsync(Guid.Empty));break;
            case 2:Check(sync?provider.LoadHolon(Guid.Empty,loadChildren:false):await provider.LoadHolonAsync(Guid.Empty,loadChildren:false));break;
            case 3:Check(sync?provider.DeleteAvatar(Guid.Empty,false):await provider.DeleteAvatarAsync(Guid.Empty,false));break;
            case 4:Check(sync?provider.DeleteAvatarDetail(Guid.Empty,false):await provider.DeleteAvatarDetailAsync(Guid.Empty,false));break;
            case 5:Check(sync?provider.DeleteHolon(Guid.Empty):await provider.DeleteHolonAsync(Guid.Empty));break;
        }
    }
}
