using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Common;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaSplMintTests
{
    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    public async Task MultipleOwnedKeysSelectActualAuthorityWithoutDependingOnOrder(int position)
    {
        var signer=new Account();var fixture=Fixture(signer);
        var keys=new List<string>{new Account().PrivateKey.Key,new Account().PrivateKey.Key};keys.Insert(position,signer.PrivateKey.Key);keys.Add(signer.PrivateKey.Key);
        fixture.Provider.Keys=new OASISResult<List<string>>{Result=keys};var original=keys.ToArray();
        var result=await fixture.Provider.MintTokenAsync(Request(fixture.Proxy));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,fixture.Proxy.Sends);
        Assert.IsTrue(signer.Verify(fixture.Proxy.Signed!.AsSpan(65).ToArray(),fixture.Proxy.Signed.AsSpan(1,64).ToArray()));
        CollectionAssert.AreEqual(original,keys,"key selection must not mutate returned wallet keys");
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task CheckedMintUsesResolvedAvatarAuthorityAndVerifiesNativeEffects(bool synchronous,bool createRecipient)
    {
        var signer=new Account();var fixture=Fixture(signer);fixture.Proxy.CreateRecipient=createRecipient;fixture.Proxy.DestinationBalance=createRecipient ? 0UL:10_000_000UL;
        var request=Request(fixture.Proxy);var result=synchronous ? fixture.Provider.MintToken(request):await fixture.Provider.MintTokenAsync(request);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,fixture.Provider.KeyLookups);Assert.AreEqual(request.MintedByAvatarId,fixture.Provider.Avatar);
        Assert.AreEqual(1,fixture.Proxy.Sends);Assert.AreEqual(1,fixture.Proxy.StatusReads);
        var transaction=Solnet.Rpc.Models.Transaction.Deserialize(fixture.Proxy.Signed!);
        Assert.AreEqual(createRecipient ? 3:2,transaction.Instructions.Count);
        var destination=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fixture.Proxy.Recipient,fixture.Proxy.Mint);
        CollectionAssert.AreEqual(TokenProgram.MintToChecked(fixture.Proxy.Mint,destination,signer.PublicKey,1_250_000,6).Data,transaction.Instructions[createRecipient ? 1:0].Data);
        Assert.IsTrue(signer.Verify(fixture.Proxy.Signed!.AsSpan(65).ToArray(),fixture.Proxy.Signed.AsSpan(1,64).ToArray()));
        Assert.AreEqual(Signature(fixture.Proxy),result.Result.TransactionResult);
    }

    [DataTestMethod]
    [DataRow("wrong-authority")] [DataRow("revoked-authority")] [DataRow("uninitialized")]
    [DataRow("mint-program")] [DataRow("destination-owner")] [DataRow("supply-overflow")] [DataRow("precision")]
    public async Task InvalidMintCannotBroadcast(string mode)
    {
        var fixture=Fixture(new Account());fixture.Proxy.Mode=mode;
        if(mode=="supply-overflow") fixture.Proxy.MintSupply=ulong.MaxValue;
        var request=Request(fixture.Proxy);if(mode=="precision") request.Amount=0.0000001m;
        var result=await fixture.Provider.MintTokenAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,fixture.Proxy.Sends);
        Assert.IsInstanceOfType(result.Exception,mode=="wrong-authority" || mode=="revoked-authority" ? typeof(UnauthorizedAccessException)
            :mode=="mint-program" ? typeof(NotSupportedException):mode=="supply-overflow" || mode=="precision" ? typeof(ArgumentException):typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DataRow("lost-response")] [DataRow("no-mint-effect")] [DataRow("no-recipient-effect")]
    public async Task SubmittedMintUncertaintyRetainsSignatureWithoutReplay(string mode)
    {
        var fixture=Fixture(new Account());fixture.Proxy.Mode=mode;fixture.Proxy.LostReply=mode=="lost-response";
        var result=await fixture.Provider.MintTokenAsync(Request(fixture.Proxy));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(1,fixture.Proxy.Sends);
        var cause=(SolanaTransactionReconciliationException)result.Exception;Assert.AreEqual(Signature(fixture.Proxy),cause.OriginalTransactionId);
        Assert.IsInstanceOfType(cause.InnerException,mode=="lost-response" ? typeof(IOException):typeof(InvalidDataException));
    }

    [DataTestMethod]
    [DataRow("denied")] [DataRow("missing")] [DataRow("malformed")]
    public async Task KeyLookupErrorsStopBeforeRpc(string mode)
    {
        var signer=new Account();var fixture=Fixture(signer);var denied=new UnauthorizedAccessException("owned authorization refusal");
        fixture.Provider.Keys=mode=="denied" ? new OASISResult<List<string>>{IsError=true,Exception=denied,ErrorCode="OWNED_KEYS_DENIED",Message="owned denied"}
            :new OASISResult<List<string>>{Result=mode=="missing" ? new List<string>():new List<string>{signer.PrivateKey.Key,"not-base58!"}};
        var result=await fixture.Provider.MintTokenAsync(Request(fixture.Proxy));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,fixture.Proxy.Calls);
        if(mode=="denied") {Assert.AreSame(denied,result.Exception);Assert.AreEqual("OWNED_KEYS_DENIED",result.ErrorCode);}
        else Assert.IsInstanceOfType(result.Exception,mode=="malformed" ? typeof(InvalidDataException):typeof(InvalidOperationException));
    }

    private static MintWeb3TokenRequest Request(SolanaSplSendTests.SplRpc rpc)=>new(){MintedByAvatarId=Guid.NewGuid(),TokenAddress=rpc.Mint.Key,ToWalletAddress=rpc.Recipient.Key,Amount=1.25m,MemoText="owned mint memo"};
    private static string Signature(SolanaSplSendTests.SplRpc rpc)=>Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(rpc.Signed!.AsSpan(1,64).ToArray());
    private static (MintProvider Provider,SolanaSplSendTests.SplRpc Proxy) Fixture(Account signer)
    {
        var rpc=DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();var proxy=(SolanaSplSendTests.SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Mint=new Account().PublicKey;proxy.Recipient=new Account().PublicKey;proxy.ApplyMint=true;proxy.MintSupply=100_000_000;
        var provider=new MintProvider(new Account()){Keys=new OASISResult<List<string>>{Result=new List<string>{signer.PrivateKey.Key}}};
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);return(provider,proxy);
    }
    private sealed class MintProvider(Account unrelated) : SolanaOASIS("http://127.0.0.1:1",unrelated.PrivateKey.Key,unrelated.PublicKey.Key)
    {
        public OASISResult<List<string>> Keys=null!;public int KeyLookups;public Guid Avatar;
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId) {KeyLookups++;Avatar=avatarId;return Keys;}
    }
}
