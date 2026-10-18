using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaNftTransferTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task LegacyNftTransferUsesOneAtomicSignedTransactionAndFinalizedReceipt(bool createRecipient,bool lost)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();var proxy=(SolanaSplSendTests.SplRpc)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;
        proxy.Decimals=0;proxy.MintSupply=1;proxy.SourceBalance=1;proxy.DestinationBalance=0;proxy.CreateRecipient=createRecipient;proxy.LostReply=lost;
        var result=await new SolanaService(new Account(),rpc).SendNftAsync(Request(owner,proxy));
        Assert.AreEqual(lost,result.IsError);Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(lost ? 0:1,proxy.StatusReads);
        var transaction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!);
        Assert.AreEqual(createRecipient ? 2:1,transaction.Instructions.Count);
        var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner.PublicKey,proxy.Mint);
        var destination=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(proxy.Recipient,proxy.Mint);
        CollectionAssert.AreEqual(TokenProgram.TransferChecked(source,destination,1,0,owner.PublicKey,proxy.Mint).Data,transaction.Instructions.Last().Data);
        Assert.IsTrue(owner.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
        var signature=Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(proxy.Signed.AsSpan(1,64).ToArray());
        if(lost){Assert.IsNull(result.Result);Assert.AreEqual(signature,((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);}
        else {Assert.IsTrue(result.IsSaved);Assert.AreEqual(signature,result.Result.TransactionHash);}
    }

    [DataTestMethod]
    [DataRow("fractional")] [DataRow("supply")] [DataRow("decimals")] [DataRow("source-owner")]
    [DataRow("source-account")] [DataRow("sender")] [DataRow("frozen")]
    public async Task InvalidNftTransferCannotCreateAccountOrBroadcast(string mode)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();var proxy=(SolanaSplSendTests.SplRpc)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;
        proxy.Decimals=mode=="decimals" ? 6:0;proxy.MintSupply=mode=="supply" ? 2UL:1;proxy.SourceBalance=1;proxy.DestinationBalance=0;
        proxy.CreateRecipient=true;proxy.Mode=mode;
        var request=Request(owner,proxy);
        if(mode=="fractional") request.Amount=0.5m;
        if(mode=="source-account") request.FromNFTTokenAddress=new Account().PublicKey.Key;
        if(mode=="sender") request.FromWalletAddress=new Account().PublicKey.Key;
        var result=await new SolanaService(new Account(),rpc).SendNftAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsNotNull(result.Exception);
        Assert.AreEqual(0,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task PublicAdapterAcceptsIndependentInterfaceRequestAndKeepsNativeReceipt(bool sync,bool lost)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();var proxy=(SolanaSplSendTests.SplRpc)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;
        proxy.Decimals=0;proxy.MintSupply=1;proxy.SourceBalance=1;proxy.DestinationBalance=0;proxy.LostReply=lost;
        var unrelated=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",unrelated.PrivateKey.Key,unrelated.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_solanaService",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,new SolanaService(unrelated,rpc));
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        var request=new IndependentRequest {FromWalletAddress=owner.PublicKey.Key,ToWalletAddress=proxy.Recipient.Key,
            TokenAddress=proxy.Mint.Key,Amount=1,OwnerPublicKey=owner.PublicKey.Key,OwnerPrivateKey=owner.PrivateKey.Key};
        var result=sync ? provider.SendNFT(request):await provider.SendNFTAsync(request);
        Assert.AreEqual(lost,result.IsError);Assert.AreEqual(!lost,result.IsSaved);Assert.AreEqual(1,proxy.Sends);
        if(lost){Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(SolanaTransactionReconciliationException));}
        else Assert.AreEqual(Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(proxy.Signed!.AsSpan(1,64).ToArray()),result.Result.TransactionResult);
    }

    private sealed class IndependentRequest:NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests.ISendWeb3NFTRequest
    {
        public string FromNFTTokenAddress {get;set;}="";public string FromWalletAddress {get;set;}="";
        public string ToWalletAddress {get;set;}="";public string TokenAddress {get;set;}="";public string TokenId {get;set;}="";
        public decimal Amount {get;set;}public string MemoText {get;set;}="";
        public string OwnerPrivateKey {get;set;}="";public string OwnerPublicKey {get;set;}="";public string OwnerSeedPhrase {get;set;}="";
    }

    private static SendWeb3NFTRequest Request(Account owner,SolanaSplSendTests.SplRpc proxy)=>new()
    {
        FromWalletAddress=owner.PublicKey.Key,ToWalletAddress=proxy.Recipient.Key,TokenAddress=proxy.Mint.Key,Amount=1,
        OwnerPrivateKey=owner.PrivateKey.Key,OwnerPublicKey=owner.PublicKey.Key
    };
}
