using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests;

[TestClass]
public class RedisReadVersionContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var kind in Enumerable.Range(0,3))foreach(var version in new[]{-1,1})foreach(var sync in new[]{false,true})
            yield return new object[]{kind,version,sync,false};
        foreach(var kind in Enumerable.Range(0,3))foreach(var sync in new[]{false,true})
            yield return new object[]{kind,1,sync,true};
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task RequestedHistoryCannotSilentlyReturnCurrentVersion(int kind,int version,bool synchronous,bool history)
    {
        var key=RandomNumberGenerator.GetBytes(32);
        try
        {
            var backend=new RedisControlledStore();
            using var provider=new RedisOASIS(backend,()=>(byte[])key.Clone()){IsVersionControlEnabled=history};
            var id=Guid.NewGuid();
            IHolonBase stored=kind==0?new Avatar{Id=id,Username="owned",Email="owned@example.invalid"}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id,IsActive=true};
            async Task Save()
            {
                void Saved<T>(OASISResult<T> result) where T:IHolonBase{Assert.IsFalse(result.IsError,result.Message);stored=result.Result;}
                if(kind==0)Saved(await provider.SaveAvatarAsync((IAvatar)stored));
                else if(kind==1)Saved(await provider.SaveAvatarDetailAsync((IAvatarDetail)stored));
                else Saved(await provider.SaveHolonAsync((IHolon)stored,saveChildren:false));
            }
            await Save();stored.Description="changed";await Save();Assert.AreEqual(2,stored.Version);
            backend.Reads=0;
            void Check<T>(OASISResult<T> result) where T:IHolonBase
            {
                if(version<0)
                {
                    Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
                    Assert.AreEqual(0,backend.Reads,"Invalid versions must fail before backend dispatch.");
                }
                else if(history)
                {
                    Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);Assert.AreEqual(1,result.Result.Version);
                    Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
                }
                else
                {
                    Assert.IsTrue(result.IsError,"Missing history must be an explicit failed point lookup, not empty success.");
                    Assert.IsNull(result.Result,"Absent history must not become current data.");Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
                }
            }
            if(kind==0)Check(synchronous?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version));
            else if(kind==1)Check(synchronous?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version));
            else Check(synchronous?provider.LoadHolon(id,loadChildren:false,version:version):await provider.LoadHolonAsync(id,loadChildren:false,version:version));
        }
        finally{CryptographicOperations.ZeroMemory(key);}
    }
}
