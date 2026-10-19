using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Common;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;
using SplAccount=Solnet.Programs.Models.TokenProgram.TokenAccount;
using SplMint=Solnet.Programs.Models.TokenProgram.TokenMint;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaTokenIssuerTests
{
    [TestMethod]
    public void SolanaIsATokenIssuer() => Assert.IsInstanceOfType(new IssuerProvider(new Account()),typeof(IOASISTokenIssuerProvider));

    [DataTestMethod]
    [DataRow(false,0)] [DataRow(true,0)] [DataRow(false,1000)] [DataRow(true,1000)]
    public async Task CreatesMintWithAvatarAuthorityAndVerifiesNativeState(bool synchronous,int supply)
    {
        var signer=new Account();var fixture=Fixture(signer);var request=Request(signer,6,supply);
        var result=synchronous ? fixture.Provider.CreateToken(request):await fixture.Provider.CreateTokenAsync(request);

        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        Assert.AreEqual(1,fixture.Rpc.Sends);Assert.AreEqual(1,fixture.Provider.KeyLookups);Assert.AreEqual(request.CreatedByAvatarId,fixture.Provider.Avatar);
        var transaction=Solnet.Rpc.Models.Transaction.Deserialize(fixture.Rpc.Signed!);
        Assert.AreEqual(supply>0 ? 4:2,transaction.Instructions.Count-1,"memo is appended after the token instructions");
        var mint=new PublicKey(fixture.Rpc.CreatedMint!);
        // Bytes 0-34: instruction, decimals, mint authority, freeze-authority option. Solnet leaves the unused
        // 32-byte freeze slot uninitialized when the option is 0, and the chain ignores it.
        CollectionAssert.AreEqual(TokenProgram.InitializeMint(mint,6,signer.PublicKey,null).Data.Take(35).ToArray(),transaction.Instructions[1].Data.Take(35).ToArray());
        Assert.AreEqual(0,transaction.Instructions[1].Data[34],"no freeze authority");
        Assert.AreEqual(2,transaction.Signatures.Count,"the authority pays and the new mint account co-signs");
        Assert.AreEqual(signer.PublicKey.Key,transaction.FeePayer.Key);
        Assert.AreEqual(mint.Key,result.Result.TokenAddress);Assert.AreEqual(6,result.Result.Decimals);Assert.AreEqual((decimal)supply,result.Result.InitialSupply);
        Assert.AreEqual(Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(fixture.Rpc.Signed!.AsSpan(1,64).ToArray()),result.Result.TransactionResult);
    }

    [TestMethod]
    public async Task AuthorityMustBeOneOfTheAvatarsWallets()
    {
        var signer=new Account();var fixture=Fixture(signer);
        var result=await fixture.Provider.CreateTokenAsync(Request(new Account(),6,0));
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(UnauthorizedAccessException));Assert.AreEqual(0,fixture.Rpc.Calls);
    }

    [DataTestMethod]
    [DataRow(-1)] [DataRow(10)]
    public async Task InvalidDecimalsStopBeforeRpc(int decimals)
    {
        var signer=new Account();var fixture=Fixture(signer);
        var result=await fixture.Provider.CreateTokenAsync(Request(signer,decimals,0));
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,fixture.Rpc.Calls);Assert.AreEqual(0,fixture.Provider.KeyLookups);
    }

    [TestMethod]
    public async Task KeyLookupFailureIsReturnedWithoutRpc()
    {
        var signer=new Account();var fixture=Fixture(signer);var denied=new UnauthorizedAccessException("owned keys denied");
        fixture.Provider.Keys=new OASISResult<List<string>>{IsError=true,Exception=denied,ErrorCode="OWNED_KEYS_DENIED",Message="denied"};
        var result=await fixture.Provider.CreateTokenAsync(Request(signer,6,0));
        Assert.IsTrue(result.IsError);Assert.AreSame(denied,result.Exception);Assert.AreEqual("OWNED_KEYS_DENIED",result.ErrorCode);Assert.AreEqual(0,fixture.Rpc.Calls);
    }

    [DataTestMethod]
    [DataRow("wrong-decimals")] [DataRow("wrong-authority")] [DataRow("no-supply")]
    public async Task SubmittedCreationThatDoesNotVerifyKeepsSignatureForReconciliation(string mode)
    {
        var signer=new Account();var fixture=Fixture(signer);fixture.Rpc.Mode=mode;
        var result=await fixture.Provider.CreateTokenAsync(Request(signer,6,mode=="no-supply" ? 1000:0));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(1,fixture.Rpc.Sends);
        var cause=(SolanaTransactionReconciliationException)result.Exception;
        Assert.AreEqual(Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(fixture.Rpc.Signed!.AsSpan(1,64).ToArray()),cause.OriginalTransactionId);
        Assert.IsInstanceOfType(cause.InnerException,typeof(InvalidDataException));
    }

    private static CreateWeb3TokenRequest Request(Account authority,int decimals,decimal supply)
        =>new(){CreatedByAvatarId=Guid.NewGuid(),AuthorityWalletAddress=authority.PublicKey.Key,Decimals=decimals,InitialSupply=supply,MemoText="owned create memo"};

    private static (IssuerProvider Provider,IssuerRpc Rpc) Fixture(Account signer)
    {
        var rpc=DispatchProxy.Create<IRpcClient,IssuerRpc>();var proxy=(IssuerRpc)(object)rpc;proxy.Owner=signer.PublicKey;
        var provider=new IssuerProvider(new Account()){Keys=new OASISResult<List<string>>{Result=new List<string>{new Account().PrivateKey.Key,signer.PrivateKey.Key}}};
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);return(provider,proxy);
    }

    private sealed class IssuerProvider(Account unrelated) : SolanaOASIS("http://127.0.0.1:1",unrelated.PrivateKey.Key,unrelated.PublicKey.Key)
    {
        public OASISResult<List<string>> Keys=null!;public int KeyLookups;public Guid Avatar;
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId) {KeyLookups++;Avatar=avatarId;return Keys;}
    }

    /// <summary>Answers rent exemption and, once the transaction is sent, reports the created mint and holder account.</summary>
    public class IssuerRpc : SolanaNativeTokenTests.TokenRpc
    {
        public PublicKey Owner=null!;public string Mode="";public string? CreatedMint;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="GetMinimumBalanceForRentExemptionAsync") {Calls++;return Respond(method,"1461600");}
            if(method.Name!="GetAccountInfoAsync") return base.Invoke(method,args);
            Calls++;var address=(string)args![0]!;
            var transaction=Solnet.Rpc.Models.Transaction.Deserialize(Signed!);
            CreatedMint=transaction.Instructions[1].Keys[0].PublicKey;
            var mintTo=transaction.Instructions.Count>4 ? transaction.Instructions[3]:null;
            ulong supply=mintTo==null || Mode=="no-supply" ? 0:BitConverter.ToUInt64(mintTo.Data,1);
            if(address==CreatedMint)
            {
                var data=new byte[SplMint.Layout.Length];
                BitConverter.GetBytes(1).CopyTo(data,SplMint.Layout.MintAuthorityOptionOffset);
                (Mode=="wrong-authority" ? new Account().PublicKey:Owner).KeyBytes.CopyTo(data,SplMint.Layout.MintAuthorityOffset);
                BitConverter.GetBytes(supply).CopyTo(data,SplMint.Layout.SupplyOffset);
                data[SplMint.Layout.DecimalsOffset]=(byte)(Mode=="wrong-decimals" ? 2:6);data[SplMint.Layout.IsInitializedOffset]=1;
                return Respond(method,JsonConvert.SerializeObject(new{Value=new{owner=TokenProgram.ProgramIdKey.Key,executable=false,data=new[]{Convert.ToBase64String(data),"base64"}}}));
            }
            var account=new byte[SplAccount.Layout.Length];
            new PublicKey(CreatedMint).KeyBytes.CopyTo(account,SplAccount.Layout.MintOffset);Owner.KeyBytes.CopyTo(account,SplAccount.Layout.OwnerOffset);
            BitConverter.GetBytes(supply).CopyTo(account,SplAccount.Layout.AmountOffset);account[SplAccount.Layout.StateOffset]=(byte)SplAccount.AccountState.Initialized;
            return Respond(method,JsonConvert.SerializeObject(new{Value=new{owner=TokenProgram.ProgramIdKey.Key,executable=false,data=new[]{Convert.ToBase64String(account),"base64"}}}));
        }
    }
}
