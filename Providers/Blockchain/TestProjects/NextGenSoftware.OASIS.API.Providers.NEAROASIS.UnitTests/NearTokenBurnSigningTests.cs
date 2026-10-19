using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearTokenBurnSigningTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnerSignerIsForwardedAndFailedBalanceFollowupRetainsReceipt(bool named)
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");var owner=Environment.GetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ACCOUNT");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            Environment.SetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ACCOUNT",named?"named.near":new string('1',64));
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-service-key");
            var request=new BurnWeb3TokenRequest{TokenAddress="symbol",Amount=1m,OwnerPrivateKey="controlled-owner-key",OwnerPublicKey="fixture-public-key",OwnerSeedPhrase=""};
            var result=named?await provider.BurnTokenAsync(request,"named.near"):await provider.BurnTokenAsync(request);
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
            var failure=result.Exception as NativeTransactionFollowupException;Assert.IsNotNull(failure);
            Assert.AreEqual("controlled-original-token-transaction",failure.OriginalTransactionId);
            Assert.IsInstanceOfType(failure.InnerException,typeof(System.IO.InvalidDataException));
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",bridge);Environment.SetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ACCOUNT",owner);}
    }

    [TestMethod]
    public async Task InsufficientBalanceStopsBeforeBurnCall()
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");var zero=Environment.GetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ZERO");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            Environment.SetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ZERO","1");
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-service-key");
            var result=await provider.BurnTokenAsync(new BurnWeb3TokenRequest{TokenAddress="symbol",Amount=1m,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=""});
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",bridge);Environment.SetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ZERO",zero);}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task InvalidScopeSeedOrMissingIndependentKeyFailsBeforeActivation(int mode)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-service-key");
        var request=new BurnWeb3TokenRequest{TokenAddress="symbol",Amount=1m,OwnerPrivateKey="",OwnerPublicKey="",OwnerSeedPhrase=mode==0?"unsupported mnemonic":""};
        var result=mode==0?await provider.BurnTokenAsync(request):await provider.BurnTokenAsync(request,mode==1?" ":"independent.near");
        Assert.IsTrue(result.IsError);Assert.IsFalse(provider.IsProviderActivated);
        if(mode==0)Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));else Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }

    [TestMethod]
    public async Task NearKeyProjectionCannotExposeAnUnrelatedBitcoinAddress()
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            var provider=new NEAROASIS();var result=await provider.GenerateKeyPairAsync();
            Assert.IsFalse(result.IsError,result.Message);Assert.IsNull(result.Result.WalletAddressSegwitP2SH);
            Assert.AreEqual("ed25519:fixture-public",result.Result.PublicKey);Assert.AreEqual("ed25519:fixture-private",result.Result.PrivateKey);
            Assert.AreEqual(new string('1',64),result.Result.WalletAddressLegacy);
        }
        finally {Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",bridge);}
    }
}
