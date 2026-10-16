using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;
using SplAccount=Solnet.Programs.Models.TokenProgram.TokenAccount;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaSplSendTests
{
    [DataTestMethod]
    [DataRow(0,"1",1UL)]
    [DataRow(18,"0.000000000000000001",1UL)]
    [DataRow(28,"0.0000000000000000000000000001",1UL)]
    [DataRow(30,"0.0000000000000000000000000001",100UL)]
    [DataRow(6,"18446744073709.551615",ulong.MaxValue)]
    public async Task ExactRawUnitsDoNotRoundOrAssumeNineDecimals(int decimals,string amount,ulong expected)
    {
        var signer=new Account();var rpc=DispatchProxy.Create<IRpcClient,SplRpc>();var proxy=(SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;proxy.Decimals=decimals;proxy.SourceBalance=ulong.MaxValue;
        var result=await Provider(rpc).SendTokenAsync(new SendWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,FromWalletAddress=signer.PublicKey.Key,
            ToWalletAddress=proxy.Recipient.Key,FromTokenAddress=proxy.Mint.Key,Amount=decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture)});
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        var instruction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!).Instructions.Single();
        var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(proxy.Owner,proxy.Mint);
        var destination=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(proxy.Recipient,proxy.Mint);
        CollectionAssert.AreEqual(TokenProgram.TransferChecked(source,destination,expected,decimals,proxy.Owner,proxy.Mint).Data,instruction.Data);
    }

    [TestMethod]
    public async Task ExcessiveRawAmountCannotBroadcast()
    {
        var signer=new Account();var rpc=DispatchProxy.Create<IRpcClient,SplRpc>();var proxy=(SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;proxy.Decimals=255;
        var result=await Provider(rpc).SendTokenAsync(new SendWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,FromWalletAddress=signer.PublicKey.Key,
            ToWalletAddress=proxy.Recipient.Key,FromTokenAddress=proxy.Mint.Key,Amount=1});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));Assert.AreEqual(0,proxy.Sends);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task CheckedSplSendUsesMintDecimalsAndOneSignedAtomicSubmission(bool synchronous,bool createRecipient)
    {
        var signer=new Account();var recipient=new Account();var mint=new Account().PublicKey;
        var rpc=DispatchProxy.Create<IRpcClient,SplRpc>();var proxy=(SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Recipient=recipient.PublicKey;proxy.Mint=mint;proxy.CreateRecipient=createRecipient;
        var provider=Provider(rpc);var request=new SendWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,FromWalletAddress=signer.PublicKey.Key,
            ToWalletAddress=recipient.PublicKey.Key,FromTokenAddress=mint.Key,Amount=1.25m};
        var result=synchronous ? provider.SendToken(request):await provider.SendTokenAsync(request);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(1,proxy.StatusReads);
        var transaction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!);
        Assert.AreEqual(createRecipient ? 2:1,transaction.Instructions.Count);
        var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(signer.PublicKey,mint);
        var destination=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(recipient.PublicKey,mint);
        CollectionAssert.AreEqual(TokenProgram.TransferChecked(source,destination,1_250_000,6,signer.PublicKey,mint).Data,transaction.Instructions.Last().Data);
        if(createRecipient) CollectionAssert.AreEqual(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(signer.PublicKey,recipient.PublicKey,mint).Data,transaction.Instructions[0].Data);
        Assert.IsTrue(signer.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
    }

    [DataTestMethod]
    [DataRow("mint-program")] [DataRow("source-owner")] [DataRow("source-mint")]
    [DataRow("frozen")] [DataRow("insufficient")] [DataRow("destination-owner")] [DataRow("precision")]
    public async Task InvalidNativeStateOrPrecisionCannotBroadcast(string mode)
    {
        var signer=new Account();var rpc=DispatchProxy.Create<IRpcClient,SplRpc>();var proxy=(SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;proxy.Mode=mode;
        var result=await Provider(rpc).SendTokenAsync(new SendWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,FromWalletAddress=signer.PublicKey.Key,
            ToWalletAddress=proxy.Recipient.Key,FromTokenAddress=proxy.Mint.Key,Amount=mode=="precision" ? 0.0000001m:1.25m});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);
        Assert.IsInstanceOfType(result.Exception,mode=="mint-program" ? typeof(NotSupportedException):mode=="insufficient" || mode=="precision" ? typeof(ArgumentException):typeof(InvalidDataException));
    }

    [TestMethod]
    public async Task SplLostResponseRetainsOriginalSignatureWithoutRebroadcast()
    {
        var signer=new Account();var rpc=DispatchProxy.Create<IRpcClient,SplRpc>();var proxy=(SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mint=new Account().PublicKey;proxy.LostReply=true;
        var result=await Provider(rpc).SendTokenAsync(new SendWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,FromWalletAddress=signer.PublicKey.Key,
            ToWalletAddress=proxy.Recipient.Key,FromTokenAddress=proxy.Mint.Key,Amount=1});
        Assert.IsTrue(result.IsError);Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);
        Assert.AreEqual(Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(proxy.Signed!.AsSpan(1,64).ToArray()),((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
    }

    private static SolanaOASIS Provider(IRpcClient rpc)
    {
        var unrelated=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",unrelated.PrivateKey.Key,unrelated.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,rpc);return provider;
    }

    public class SplRpc : SolanaNativeTokenTests.TokenRpc
    {
        public PublicKey Owner=null!,Recipient=null!,Mint=null!;public string Mode="";public bool CreateRecipient;public int Decimals=6;public ulong SourceBalance=10_000_000;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="GetTokenSupplyAsync") {Calls++;return Respond(method,JsonConvert.SerializeObject(new{Value=new{decimals=Decimals,amount=ulong.MaxValue.ToString()}}));}
            if(method.Name!="GetAccountInfoAsync") return base.Invoke(method,args);
            Calls++;var address=(string)args![0]!;
            if(address==Mint.Key) return Respond(method,JsonConvert.SerializeObject(new{Value=new{owner=Mode=="mint-program" ? new Account().PublicKey.Key:TokenProgram.ProgramIdKey.Key,
                executable=false,data=new[]{Convert.ToBase64String(new byte[TokenProgram.MintAccountDataSize]),"base64"}}}));
            var source=address==AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(Owner,Mint).Key;
            if(!source && CreateRecipient) return Respond(method,"{\"Value\":null}");
            var data=new byte[SplAccount.Layout.Length];
            (source && Mode=="source-mint" ? new Account().PublicKey:Mint).KeyBytes.CopyTo(data,SplAccount.Layout.MintOffset);
            (source ? Mode=="source-owner" ? new Account().PublicKey:Owner:Mode=="destination-owner" ? new Account().PublicKey:Recipient).KeyBytes.CopyTo(data,SplAccount.Layout.OwnerOffset);
            BitConverter.GetBytes(source ? Mode=="insufficient" ? 1UL:SourceBalance:10_000_000UL).CopyTo(data,SplAccount.Layout.AmountOffset);
            data[SplAccount.Layout.StateOffset]=(byte)(Mode=="frozen" && source ? SplAccount.AccountState.Frozen:SplAccount.AccountState.Initialized);
            return Respond(method,JsonConvert.SerializeObject(new{Value=new{owner=TokenProgram.ProgramIdKey.Key,executable=false,data=new[]{Convert.ToBase64String(data),"base64"}}}));
        }
    }
}
