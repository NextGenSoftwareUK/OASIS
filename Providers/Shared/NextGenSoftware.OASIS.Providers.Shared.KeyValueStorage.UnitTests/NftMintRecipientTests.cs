using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NftMintRecipientTests
{
    private static OASISResult<IProviderWallet> Wallet(string address)=>new(){Result=new ProviderWallet{WalletAddress=address}};
    private static MintWeb3NFTRequest Request(string selector)=>new(){
        SendToAvatarAfterMintingId=selector is "id" or "all"?Guid.NewGuid():Guid.Empty,
        SendToAvatarAfterMintingUsername=selector is "username" or "all"?"recipient":null,
        SendToAvatarAfterMintingEmail=selector is "email" or "all"?"recipient@example.invalid":null};

    [DataTestMethod][DataRow("id")][DataRow("username")][DataRow("email")][DataRow("all")]
    public async Task SelectedWalletLookupsResolveOneRecipient(string selector)
    {
        var calls=0;Task<OASISResult<IProviderWallet>> Read(){calls++;return Task.FromResult(Wallet("recipient-address"));}
        var result=await NftMintRecipient.ResolveAsync(Request(selector),"minter",_=>Read(),_=>Read(),_=>Read());
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual("recipient-address",result.Result);
        Assert.AreEqual(selector=="all"?3:1,calls);
    }
    public static IEnumerable<object[]> Failures()=>from selector in new[]{"id","username","email","all"}
        from mode in new[]{"error","throw","missing-envelope","missing-wallet","empty-address"} select new object[]{selector,mode};
    [DataTestMethod][DynamicData(nameof(Failures),DynamicDataSourceType.Method)]
    public async Task FailedRecipientNeverFallsBackToMinterOrAnotherLookup(string selector,string mode)
    {
        var cause=new IOException("controlled recipient read failure");var calls=0;
        Task<OASISResult<IProviderWallet>> Read(){
            calls++;
            if(mode=="throw")return Task.FromException<OASISResult<IProviderWallet>>(cause);
            return Task.FromResult(mode switch{
                "error"=>new OASISResult<IProviderWallet>{IsError=true,Exception=cause,ErrorCode="CONTROLLED",DetailedMessage="owned diagnostics"},
                "missing-envelope"=>null,"missing-wallet"=>new OASISResult<IProviderWallet>(),_=>Wallet("")});
        }
        var result=await NftMintRecipient.ResolveAsync(Request(selector),"minter",_=>Read(),_=>Read(),_=>Read());
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(1,calls);
        if(mode is "error" or "throw")Assert.AreSame(cause,result.Exception);
        if(mode=="error"){Assert.AreEqual("CONTROLLED",result.ErrorCode);Assert.AreEqual("owned diagnostics",result.DetailedMessage);}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DefaultMinterAndDirectAddressRequireNoRecipientLookup(bool direct)
    {
        var request=new MintWeb3NFTRequest{SendToAddressAfterMinting=direct?"explicit-recipient":null};var calls=0;
        Task<OASISResult<IProviderWallet>> Read(){calls++;throw new InvalidOperationException();}
        var result=await NftMintRecipient.ResolveAsync(request,"minter",_=>Read(),_=>Read(),_=>Read());
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(direct?"explicit-recipient":"minter",result.Result);Assert.AreEqual(0,calls);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ProviderComparerDeterminesEquivalenceWithoutChangingSelectedAddress(bool equivalent)
    {
        var request=Request("all");request.SendToAddressAfterMinting="ADDRESS";
        var result=await NftMintRecipient.ResolveAsync(request,"minter",_=>Task.FromResult(Wallet("address")),
            _=>Task.FromResult(Wallet("address")),_=>Task.FromResult(Wallet("address")),
            equivalent?(left,right)=>StringComparer.OrdinalIgnoreCase.Equals(left,right):null);
        Assert.AreEqual(!equivalent,result.IsError);Assert.AreEqual(equivalent?"ADDRESS":null,result.Result);
    }
    [TestMethod]
    public async Task SelectorsAreCapturedBeforeFirstWalletAwait()
    {
        var request=Request("all");request.SendToAddressAfterMinting="recipient-address";
        var originalId=request.SendToAvatarAfterMintingId;
        var first=new TaskCompletionSource<OASISResult<IProviderWallet>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending=NftMintRecipient.ResolveAsync(request,"minter",id=>{Assert.AreEqual(originalId,id);return first.Task;},
            username=>{Assert.AreEqual("recipient",username);return Task.FromResult(Wallet("recipient-address"));},
            email=>{Assert.AreEqual("recipient@example.invalid",email);return Task.FromResult(Wallet("recipient-address"));});
        Assert.IsFalse(pending.IsCompleted);
        request.SendToAvatarAfterMintingId=Guid.NewGuid();request.SendToAvatarAfterMintingUsername="changed";
        request.SendToAvatarAfterMintingEmail="changed";request.SendToAddressAfterMinting="changed";
        first.SetResult(Wallet("recipient-address"));var result=await pending;
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual("recipient-address",result.Result);
    }
}
