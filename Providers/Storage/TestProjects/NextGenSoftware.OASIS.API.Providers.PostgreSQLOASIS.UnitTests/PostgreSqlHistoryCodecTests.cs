using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlHistoryCodecTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task NativeKeyCannotReturnADifferentCanonicalRecord(int kind)
    {
        if(kind==0)await CheckKey<IAvatar>(new Avatar{Id=Guid.NewGuid()});
        else if(kind==1)await CheckKey<IAvatarDetail>(new AvatarDetail{Id=Guid.NewGuid()});
        else await CheckKey<IHolon>(new Holon{Id=Guid.NewGuid()});
    }
    private static async Task CheckKey<T>(T source) where T:class,IHolonBase
    {
        var read=typeof(PostgreSQLOASIS).GetMethod("ReadKeyedStoredRecordAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        var json=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(source);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>await (Task<T>)read.Invoke(null,new object[]{Guid.NewGuid(),json,0})!);
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)][DataRow(5,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)][DataRow(5,true)]
    public async Task NegativeVersionIsRejectedBeforeAnyNativeConnection(int kind,bool synchronous)
    {
        var provider=new PostgreSQLOASIS("UnsupportedConnectionKeyword=true");var id=Guid.NewGuid();
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);}
        if(kind==0)Check(synchronous?provider.LoadAvatar(id,-1):await provider.LoadAvatarAsync(id,-1));
        else if(kind==1)Check(synchronous?provider.LoadAvatarDetail(id,-1):await provider.LoadAvatarDetailAsync(id,-1));
        else if(kind==2)Check(synchronous?provider.LoadHolon(id,version:-1):await provider.LoadHolonAsync(id,version:-1));
        else if(kind==3)Check(synchronous?provider.LoadAllAvatars(-1):await provider.LoadAllAvatarsAsync(-1));
        else if(kind==4)Check(synchronous?provider.LoadAllAvatarDetails(-1):await provider.LoadAllAvatarDetailsAsync(-1));
        else Check(synchronous?provider.LoadAllHolons(version:-1):await provider.LoadAllHolonsAsync(version:-1));
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task HistoryAdapterPreservesCanonicalPayload(int kind)
    {
        if(kind==0)await Check<IAvatar>(new Avatar{Id=Guid.NewGuid(),FirstName="original"});
        else if(kind==1)await Check<IAvatarDetail>(new AvatarDetail{Id=Guid.NewGuid(),FirstName="original"});
        else await Check<IHolon>(new Holon{Id=Guid.NewGuid(),Name="original"});
    }
    private static async Task Check<T>(T source) where T:class,IHolonBase
    {
        var provider=new PostgreSQLOASIS("Unused=true"){IsVersionControlEnabled=true};
        var stage=typeof(PostgreSQLOASIS).GetMethod("StageStoredRecordAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        var first=await (Task<(string Json,T Record)>)stage.Invoke(provider,new object?[]{source,null})!;
        Assert.AreEqual("original",Label(first.Record));
        if(source is IAvatar avatar)avatar.FirstName="second";else if(source is AvatarDetail detail)detail.FirstName="second";else source.Name="second";
        var second=await (Task<(string Json,T Record)>)stage.Invoke(provider,new object?[]{source,first.Json})!;
        var read=typeof(PostgreSQLOASIS).GetMethod("ReadStoredRecordAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        var historical=await (Task<T>)read.Invoke(null,new object[]{second.Json,1})!;
        Assert.AreEqual("original",Label(historical));
    }
    private static string Label(IHolonBase record)=>record is IAvatar avatar?avatar.FirstName:record is AvatarDetail detail?detail.FirstName:record.Name;
}
