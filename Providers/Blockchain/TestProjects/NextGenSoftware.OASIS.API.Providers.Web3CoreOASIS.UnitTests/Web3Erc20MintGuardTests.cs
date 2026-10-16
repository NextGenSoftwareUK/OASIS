using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
public sealed class Web3Erc20MintGuardTests
{
    [DataTestMethod]
    [DataRow("missing-token")] [DataRow("missing-key")] [DataRow("zero")] [DataRow("negative")]
    public async Task InvalidBurnCannotSubstituteBalanceOrOneToken(string mode)
    {
        var provider=new GuardProvider();var request=new BurnWeb3TokenRequest{TokenAddress=mode=="missing-token" ? null!:"0x"+new string('1',40),
            OwnerPrivateKey=mode=="missing-key" ? null!:"0x"+new string('1',64),OwnerPublicKey="",OwnerSeedPhrase="",Amount=mode=="zero" ? 0:mode=="negative" ? -1:2};
        var result=await provider.BurnTokenAsync(request);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsFalse(provider.IsProviderActivated);Assert.AreEqual(0,provider.KeyCalls);
    }

    [DataTestMethod]
    [DataRow("1.234567",6,"1234567")]
    [DataRow("1.25",30,"1250000000000000000000000000000")]
    [DataRow("1",0,"1")]
    public void SharedExactUnitsNeverRoundOrCastThroughDecimalMultiplier(string amount,int decimals,string expected)
        =>Assert.AreEqual(System.Numerics.BigInteger.Parse(expected),NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ExactEvmTokenAmount.ToUnits(decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture),(byte)decimals));

    [DataTestMethod]
    [DataRow("0.0000001",6)] [DataRow("2",77)] [DataRow("0",18)]
    public void SharedExactUnitsRejectPrecisionLossOverflowAndZero(string amount,int decimals)
    {
        void Convert()=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ExactEvmTokenAmount.ToUnits(decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture),(byte)decimals);
        if(amount=="0.0000001") Assert.ThrowsException<ArgumentException>(Convert);
        else Assert.ThrowsException<ArgumentOutOfRangeException>(Convert);
    }

    [DataTestMethod]
    [DataRow("missing-token")] [DataRow("missing-recipient")] [DataRow("zero-recipient")] [DataRow("zero-amount")] [DataRow("missing-avatar")]
    public async Task InvalidMintCannotUseConfiguredDefaultsOrReachSigner(string mode)
    {
        var provider=new GuardProvider();var request=Request();
        if(mode=="missing-token") request.TokenAddress=null!;
        if(mode=="missing-recipient") request.ToWalletAddress=null!;
        if(mode=="zero-recipient") request.ToWalletAddress="0x"+new string('0',40);
        if(mode=="zero-amount") request.Amount=0;
        if(mode=="missing-avatar") request.MintedByAvatarId=Guid.Empty;
        var result=await provider.MintTokenAsync(request);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.AreEqual(0,provider.KeyCalls);Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task SigningAuthorizationFailurePreservesOriginalCauseBeforeActivation()
    {
        var provider=new GuardProvider();var result=await provider.MintTokenAsync(Request());
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreSame(provider.Cause,result.Exception);
        Assert.AreEqual("OWNED_KEY_DENIED",result.ErrorCode);Assert.IsFalse(provider.IsProviderActivated);
    }

    private static MintWeb3TokenRequest Request()=>new(){MintedByAvatarId=Guid.NewGuid(),Amount=2,TokenAddress="0x"+new string('1',40),ToWalletAddress="0x"+new string('2',40)};
    private sealed class GuardProvider():Web3CoreOASISBaseProvider("http://127.0.0.1:1","0x"+new string('1',64),"0x"+new string('3',40),31337)
    {
        public int KeyCalls;public readonly UnauthorizedAccessException Cause=new("owned key denial");
        protected override OASISResult<List<string>> ResolveErc20MintSigningKeys(Guid avatarId)
        {KeyCalls++;return new(){IsError=true,Message="owned denied",Exception=Cause,ErrorCode="OWNED_KEY_DENIED"};}
    }
}
