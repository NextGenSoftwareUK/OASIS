using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;
using Selector = NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.SolanaOASIS.AvatarTransferSelector;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaAvatarTransferTests
{
    private sealed class WalletProvider(Account service) : SolanaOASIS("http://127.0.0.1:1", service.PrivateKey.Key, service.PublicKey.Key)
    {
        internal readonly Account Sender = new(), Recipient = new();
        internal readonly List<(Selector Selector, string Identity, bool Signing)> Reads = new();
        internal string Failure = "";
        internal readonly Exception LookupFailure = new IOException("synthetic wallet lookup failure");
        internal override Task<OASISResult<IProviderWallet>> LoadTransferWalletAsync(Selector selector, string identity, bool signing)
        {
            Reads.Add((selector, identity, signing));
            if (Failure == "lookup") return Task.FromResult(new OASISResult<IProviderWallet> { IsError = true, ErrorCode = "OWNED_LOOKUP_ERROR", Exception = LookupFailure });
            var account = signing ? Sender : Recipient;
            return Task.FromResult(new OASISResult<IProviderWallet> { IsLoaded = true, Result = new ProviderWallet
            {
                ProviderType = Core.Enums.ProviderType.SolanaOASIS, PublicKey = account.PublicKey.Key,
                WalletAddress = Failure == "address" ? new Account().PublicKey.Key : account.PublicKey.Key,
                PrivateKey = signing ? Failure == "key" ? service.PrivateKey.Key : account.PrivateKey.Key : null
            } });
        }
    }

    private static Task<OASISResult<ITransactionResponse>> Send(WalletProvider p, string selector, bool sync, decimal amount, string? token)
    {
        var from = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var to = Guid.Parse("22222222-2222-2222-2222-222222222222");
        if (token == null) return selector switch
        {
            "Id" => sync ? Task.FromResult(p.SendTransactionById(from,to,amount)) : p.SendTransactionByIdAsync(from,to,amount),
            "Username" => sync ? Task.FromResult(p.SendTransactionByUsername("sender","recipient",amount)) : p.SendTransactionByUsernameAsync("sender","recipient",amount),
            "Email" => sync ? Task.FromResult(p.SendTransactionByEmail("sender@example.invalid","recipient@example.invalid",amount)) : p.SendTransactionByEmailAsync("sender@example.invalid","recipient@example.invalid",amount),
            _ => throw new ArgumentException()
        };
        return selector switch
        {
            "Id" => sync ? Task.FromResult(p.SendTransactionById(from,to,amount,token!)) : p.SendTransactionByIdAsync(from,to,amount,token!),
            "Username" => sync ? Task.FromResult(p.SendTransactionByUsername("sender","recipient",amount,token!)) : p.SendTransactionByUsernameAsync("sender","recipient",amount,token!),
            "Email" => sync ? Task.FromResult(p.SendTransactionByEmail("sender@example.invalid","recipient@example.invalid",amount,token!)) : p.SendTransactionByEmailAsync("sender@example.invalid","recipient@example.invalid",amount,token!),
            _ => throw new ArgumentException()
        };
    }

    [DataTestMethod]
    [DataRow("Id",false,false)] [DataRow("Id",true,false)] [DataRow("Id",false,true)] [DataRow("Id",true,true)]
    [DataRow("Username",false,false)] [DataRow("Username",true,false)] [DataRow("Username",false,true)] [DataRow("Username",true,true)]
    [DataRow("Email",false,false)] [DataRow("Email",true,false)] [DataRow("Email",false,true)] [DataRow("Email",true,true)]
    public async Task AvatarSelectorsReuseSenderSigningAndExactNativeOrMintUnits(string selector, bool sync, bool spl)
    {
        var p = new WalletProvider(new Account());
        var rpc = DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();
        var capture = (SolanaSplSendTests.SplRpc)(object)rpc;
        capture.Owner=p.Sender.PublicKey; capture.Recipient=p.Recipient.PublicKey; capture.Mint=new Account().PublicKey;
        capture.Decimals=6;
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(p,rpc);
        var result=await Send(p,selector,sync,spl ? 0.000001m : 0.000000001m,spl ? capture.Mint.Key : null);
        Assert.IsFalse(result.IsError,result.Exception?.ToString()); Assert.IsTrue(result.IsSaved); Assert.AreEqual(1,capture.Sends);
        Assert.AreEqual(2,p.Reads.Count); Assert.IsTrue(p.Reads[0].Signing); Assert.IsFalse(p.Reads[1].Signing);
        Assert.AreEqual(Enum.Parse<Selector>(selector),p.Reads[0].Selector); Assert.AreEqual(p.Reads[0].Selector,p.Reads[1].Selector);
        Assert.AreEqual(selector == "Id" ? "11111111-1111-1111-1111-111111111111" : selector == "Username" ? "sender" : "sender@example.invalid",p.Reads[0].Identity);
        Assert.AreEqual(selector == "Id" ? "22222222-2222-2222-2222-222222222222" : selector == "Username" ? "recipient" : "recipient@example.invalid",p.Reads[1].Identity);
        Assert.IsTrue(p.Sender.PublicKey.Verify(capture.Signed!.AsSpan(65).ToArray(),capture.Signed.AsSpan(1,64).ToArray()));
        var instruction=Solnet.Rpc.Models.Transaction.Deserialize(capture.Signed).Instructions.Single();
        var expected=spl ? TokenProgram.TransferChecked(AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(p.Sender.PublicKey,capture.Mint),
            AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(p.Recipient.PublicKey,capture.Mint),1,6,p.Sender.PublicKey,capture.Mint).Data
            : SystemProgram.Transfer(p.Sender.PublicKey,p.Recipient.PublicKey,1).Data;
        CollectionAssert.AreEqual(expected,instruction.Data);
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task DefaultWalletAliasUsesTheActualSenderAndExactLamports(bool sync)
    {
        var p=new WalletProvider(new Account());
        var rpc=DispatchProxy.Create<IRpcClient,SolanaNativeTokenTests.TokenRpc>();
        var capture=(SolanaNativeTokenTests.TokenRpc)(object)rpc;
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(p,rpc);
        var from=Guid.NewGuid();var to=Guid.NewGuid();
        var result=sync ? p.SendTransactionByDefaultWallet(from,to,0.000000001m) : await p.SendTransactionByDefaultWalletAsync(from,to,0.000000001m);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,capture.Sends);
        Assert.AreEqual(from.ToString(),p.Reads[0].Identity);Assert.AreEqual(to.ToString(),p.Reads[1].Identity);
        Assert.IsTrue(p.Sender.PublicKey.Verify(capture.Signed!.AsSpan(65).ToArray(),capture.Signed.AsSpan(1,64).ToArray()));
        CollectionAssert.AreEqual(SystemProgram.Transfer(p.Sender.PublicKey,p.Recipient.PublicKey,1).Data,
            Solnet.Rpc.Models.Transaction.Deserialize(capture.Signed).Instructions.Single().Data);
    }

    [DataTestMethod]
    [DataRow("Id","key")] [DataRow("Username","key")] [DataRow("Email","key")]
    [DataRow("Id","address")] [DataRow("Username","address")] [DataRow("Email","address")]
    [DataRow("Id","lookup")] [DataRow("Username","lookup")] [DataRow("Email","lookup")]
    public async Task InvalidSenderStopsBeforeRecipientOrChain(string selector,string failure)
    {
        var p=new WalletProvider(new Account()) { Failure=failure };
        var result=await Send(p,selector,false,1,new Account().PublicKey.Key);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(1,p.Reads.Count);
        if(failure=="lookup") { Assert.AreSame(p.LookupFailure,result.Exception); Assert.AreEqual("OWNED_LOOKUP_ERROR",result.ErrorCode); }
    }

    [DataTestMethod] [DataRow("Id")] [DataRow("Username")] [DataRow("Email")]
    public async Task InvalidAmountStopsBeforeWalletLookup(string selector)
    {
        var p=new WalletProvider(new Account());
        var result=await Send(p,selector,false,0,null);
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,p.Reads.Count);
    }
}
