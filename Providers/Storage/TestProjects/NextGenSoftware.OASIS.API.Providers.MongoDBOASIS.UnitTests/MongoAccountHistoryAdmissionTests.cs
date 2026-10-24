using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoAccountHistoryAdmissionTests
{
    [DataTestMethod]
    [DataRow(false,false,0)] [DataRow(false,true,0)] [DataRow(true,false,0)] [DataRow(true,true,0)]
    [DataRow(false,false,1)] [DataRow(false,true,1)] [DataRow(true,false,1)] [DataRow(true,true,1)]
    [DataRow(false,false,2)] [DataRow(false,true,2)] [DataRow(true,false,2)] [DataRow(true,true,2)]
    [DataRow(false,false,3)] [DataRow(false,true,3)] [DataRow(true,false,3)] [DataRow(true,true,3)]
    [DataRow(false,false,4)] [DataRow(false,true,4)] [DataRow(true,false,4)] [DataRow(true,true,4)]
    public async Task InvalidHistoricalSelectorsRejectBeforeActivation(bool detail,bool synchronous,int kind)
    {
        var provider=new MongoDBOASIS("invalid-no-network-connection","history-admission-no-io");
        var id=kind==1?Guid.Empty:Guid.NewGuid();var version=kind==0?-1:1;
        if(detail)
        {
            var result=kind switch {
                2=>synchronous?provider.LoadAvatarDetailByUsername(" ",version):await provider.LoadAvatarDetailByUsernameAsync(" ",version),
                3=>synchronous?provider.LoadAvatarDetailByEmail(" ",version):await provider.LoadAvatarDetailByEmailAsync(" ",version),
                _=>synchronous?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version)};
            Check(result,kind==4?"MONGO_HISTORY_READ_FAILED":"HYPERDRIVE_PAYLOAD_INVALID");
        }
        else
        {
            var result=kind switch {
                2=>synchronous?provider.LoadAvatarByUsername(" ",version):await provider.LoadAvatarByUsernameAsync(" ",version),
                3=>synchronous?provider.LoadAvatarByProviderKey("invalid-key",version):await provider.LoadAvatarByProviderKeyAsync("invalid-key",version),
                _=>synchronous?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version)};
            Check(result,kind==4?"MONGO_HISTORY_READ_FAILED":"HYPERDRIVE_PAYLOAD_INVALID");
        }
        Assert.IsFalse(provider.IsProviderActivated);
    }
    private static void Check<T>(OASISResult<T> result,string code)
    {
        Assert.IsTrue(result.IsError);Assert.AreEqual(code,result.ErrorCode);Assert.IsNull(result.Result);
        Assert.IsNotNull(result.Exception);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
}
