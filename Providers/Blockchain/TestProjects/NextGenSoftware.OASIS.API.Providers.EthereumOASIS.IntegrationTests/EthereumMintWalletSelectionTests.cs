using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public sealed class EthereumMintWalletSelectionTests
{
    [DataTestMethod]
    [DataRow("missing")] [DataRow("unowned")] [DataRow("denied")]
    public async Task InvalidDefaultCannotActivateOrSelectFirstAuthorizedKey(string mode)
    {
        var provider=new SelectionProvider();
        provider.Default=mode=="denied" ? new OASISResult<string>{IsError=true,ErrorCode="OWNED_DEFAULT_DENIED",Exception=provider.Cause,Message="owned denial"}
            :new OASISResult<string>{Result=mode=="missing" ? null:new Account(Nethereum.Signer.EthECKey.GenerateKey().GetPrivateKey()).Address};
        var result=await provider.MintTokenAsync(Request());
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,provider.Activations);Assert.AreEqual(1,provider.DefaultReads);
        if(mode=="denied") {Assert.AreSame(provider.Cause,result.Exception);Assert.AreEqual("OWNED_DEFAULT_DENIED",result.ErrorCode);}
        else Assert.IsInstanceOfType(result.Exception,mode=="missing" ? typeof(InvalidOperationException):typeof(UnauthorizedAccessException));
    }

    [TestMethod]
    public async Task DuplicateKeyAliasesRemainOneAccountWithoutDefaultLookup()
    {
        var provider=new SelectionProvider();provider.Keys=new List<string>{provider.Key,provider.Key[2..]};
        var result=await provider.MintTokenAsync(Request());
        Assert.IsTrue(result.IsError);Assert.AreSame(provider.Cause,result.Exception);
        Assert.AreEqual(1,provider.Activations);Assert.AreEqual(0,provider.DefaultReads);
    }

    private static MintWeb3TokenRequest Request()=>new(){MintedByAvatarId=Guid.NewGuid(),Amount=1,TokenAddress="0x"+new string('1',40),ToWalletAddress="0x"+new string('2',40)};
    private sealed class SelectionProvider : EthereumOASIS
    {
        public readonly string Key;public List<string> Keys;public OASISResult<string> Default=new();
        public readonly IOException Cause=new("owned activation/default refusal");public int Activations,DefaultReads;
        public SelectionProvider():base("http://127.0.0.1:1","0x"+new string('1',64),31337,"0x"+new string('1',40))
        {
            Key="0x"+new string('1',64);Keys=new(){Key,Nethereum.Signer.EthECKey.GenerateKey().GetPrivateKey()};
        }
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId)=>new(){Result=Keys};
        protected override Task<OASISResult<string>> ResolveDefaultTokenMintWalletAsync(Guid avatarId){DefaultReads++;return Task.FromResult(Default);}
        public override Task<OASISResult<bool>> ActivateProviderAsync(){Activations++;return Task.FromResult(new OASISResult<bool>{IsError=true,Exception=Cause,Message="owned activation refusal"});}
    }
}
