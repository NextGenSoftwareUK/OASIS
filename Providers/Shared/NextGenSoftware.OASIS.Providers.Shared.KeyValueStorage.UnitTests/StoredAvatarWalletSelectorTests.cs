using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StoredAvatarWalletSelectorTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ValidationRunsBeforeStorage(bool privateKey)
    {
        var calls=0;
        Task<OASISResult<IEnumerable<IAvatar>>> Read(int version) {calls++;throw new InvalidOperationException();}
        foreach(var input in new[] {(" ",0),("synthetic",-1)})
        {
            var result=await StoredAvatarWalletSelector.SelectAsync(input.Item1,privateKey,input.Item2,Read);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
            Assert.IsNull(result.Result);
        }
        Assert.AreEqual(0,calls);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeErrorAndThrownCauseRemainOriginal(bool privateKey)
    {
        var cause=new InvalidOperationException("owned storage failure");
        var source=new OASISResult<IEnumerable<IAvatar>> {IsError=true,Exception=cause,ErrorCode="OWNED_NATIVE_ERROR",Message="native failed"};
        var result=await StoredAvatarWalletSelector.SelectAsync("synthetic",privateKey,0,_=>Task.FromResult(source));
        Assert.AreSame(cause,result.Exception);Assert.AreEqual(source.ErrorCode,result.ErrorCode);Assert.IsNull(result.Result);
        result=await StoredAvatarWalletSelector.SelectAsync("synthetic",privateKey,0,_=>throw cause);
        Assert.IsTrue(result.IsError);Assert.AreSame(cause,result.Exception);Assert.IsFalse(result.IsLoaded);
    }
}
