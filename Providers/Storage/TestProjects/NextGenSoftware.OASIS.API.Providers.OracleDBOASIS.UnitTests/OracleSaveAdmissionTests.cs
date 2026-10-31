using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleSaveAdmissionTests
{
    [DataTestMethod]
    [DataRow(false,false,false,null)][DataRow(false,false,false,"")]
    [DataRow(false,false,true,null)][DataRow(false,false,true,"")]
    [DataRow(false,true,false,null)][DataRow(false,true,false,"")]
    [DataRow(false,true,true,null)][DataRow(false,true,true,"")]
    [DataRow(true,false,false,null)][DataRow(true,false,false,"")]
    [DataRow(true,false,true,null)][DataRow(true,false,true,"")]
    [DataRow(true,true,false,null)][DataRow(true,true,false,"")]
    [DataRow(true,true,true,null)][DataRow(true,true,true,"")]
    public async Task RequiredNativeIdentityRejectsBeforeConnectionWithoutPublishingCallerState(bool detail,bool synchronous,bool email,string? absent)
    {
        var provider=Provider();var username=email?"owned-username":absent;var address=email?absent:"owned@example.invalid";
        IHolonBase row=detail?new AvatarDetail{Username=username,Email=address}:new Avatar{Username=username,Email=address};
        row.Id=Guid.Empty;row.Version=9;row.VersionId=Guid.NewGuid();var originalVersion=row.VersionId;
        row.ProviderUniqueStorageKey=new(){[ProviderType.MongoDBOASIS]="owned-existing-provider-key"};var originalKeys=row.ProviderUniqueStorageKey;
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            Assert.AreEqual(email?"Email":"Username",((ArgumentException)result.Exception).ParamName);
        }
        if(detail)Check(synchronous?provider.SaveAvatarDetail((IAvatarDetail)row):await provider.SaveAvatarDetailAsync((IAvatarDetail)row));
        else Check(synchronous?provider.SaveAvatar((IAvatar)row):await provider.SaveAvatarAsync((IAvatar)row));
        Assert.AreEqual(Guid.Empty,row.Id);Assert.AreEqual(9,row.Version);Assert.AreEqual(originalVersion,row.VersionId);
        Assert.AreSame(originalKeys,row.ProviderUniqueStorageKey);Assert.AreEqual(1,originalKeys.Count);
        Assert.AreEqual(username,detail?((IAvatarDetail)row).Username:((IAvatar)row).Username);
        Assert.AreEqual(address,detail?((IAvatarDetail)row).Email:((IAvatar)row).Email);
    }

    private static OracleDBOASIS Provider()=>new("UnsupportedOracleOptionForOwnedTest=true");

    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task InvalidNativeConnectionDoesNotPublishCallerIdentityOrStorageKey(int kind,bool synchronous)
    {
        var provider=Provider();IHolonBase row=kind==0?new Avatar{Username="owned",Email="owned@example.invalid"}:kind==1?new AvatarDetail{Username="owned",Email="owned@example.invalid"}:new Holon();
        row.Id=Guid.Empty;row.Version=9;row.VersionId=Guid.NewGuid();var version=row.VersionId;
        row.ProviderUniqueStorageKey=new(){[ProviderType.MongoDBOASIS]="owned-existing-provider-key"};var originalKeys=row.ProviderUniqueStorageKey;
        if(kind==0)Check(synchronous?provider.SaveAvatar((IAvatar)row):await provider.SaveAvatarAsync((IAvatar)row));
        else if(kind==1)Check(synchronous?provider.SaveAvatarDetail((IAvatarDetail)row):await provider.SaveAvatarDetailAsync((IAvatarDetail)row));
        else Check(synchronous?provider.SaveHolon((IHolon)row,saveChildren:false):await provider.SaveHolonAsync((IHolon)row,saveChildren:false));
        Assert.AreEqual(Guid.Empty,row.Id);Assert.AreEqual(9,row.Version);Assert.AreEqual(version,row.VersionId);
        Assert.AreSame(originalKeys,row.ProviderUniqueStorageKey);Assert.AreEqual(1,row.ProviderUniqueStorageKey.Count);
        Assert.AreEqual("owned-existing-provider-key",row.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            Assert.IsTrue(result.Exception.Message.Contains("UnsupportedOracleOptionForOwnedTest",StringComparison.OrdinalIgnoreCase),"Reach the real SDK connection-string rejection, not an earlier snapshot/serialization failure.");
        }
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task NullRecordsReturnOriginalArgumentErrorWithoutCatchPathFailure(int kind,bool synchronous)
    {
        var provider=Provider();
        if(kind==0)Check(synchronous?provider.SaveAvatar(null!):await provider.SaveAvatarAsync(null!));
        else if(kind==1)Check(synchronous?provider.SaveAvatarDetail(null!):await provider.SaveAvatarDetailAsync(null!));
        else Check(synchronous?provider.SaveHolon(null!,saveChildren:false):await provider.SaveHolonAsync(null!,saveChildren:false));
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentNullException>(result.Exception);}
    }
}
