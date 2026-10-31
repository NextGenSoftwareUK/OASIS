using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearTokenSendValidationTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task MnemonicTransferForwardsOwnerAndRetainsAcknowledgementOnFailedBookkeeping(bool native,bool named)
    {
        var bridge=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");var previousOwner=Environment.GetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ACCOUNT");
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            var owner=named?"named-owner.near":new string('1',64);Environment.SetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ACCOUNT",owner);
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","configured-service-key");
            var sent=await provider.SendTokenAsync(new SendWeb3TokenRequest{FromTokenAddress=native?"NEAR":"owned-symbol",FromWalletAddress=named?owner:"",
                ToWalletAddress="receiver.near",Amount=1,OwnerPrivateKey="",OwnerPublicKey="fixture-public-key",
                OwnerSeedPhrase="abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"});
            Assert.IsTrue(sent.IsError);Assert.IsNull(sent.Result);
            var failure=sent.Exception as NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.NativeTransactionFollowupException;
            Assert.IsNotNull(failure);Assert.AreEqual("controlled-original-send-transaction",failure.OriginalTransactionId);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
        }finally{Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",bridge);Environment.SetEnvironmentVariable("OASIS_NEAR_TOKEN_TEST_ACCOUNT",previousOwner);}
    }

    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task CallerSignerInputsCannotBeIgnoredForNativeOrCustomTransfer(bool native,int mode)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        try{
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","configured-service-key");
            var request=new SendWeb3TokenRequest{FromTokenAddress=native?"NEAR":"owned-symbol",ToWalletAddress="receiver.near",Amount=1,
                OwnerPrivateKey=mode==2?"explicit-key":"",OwnerPublicKey=mode==1?"wrong-public-key":"",
                OwnerSeedPhrase=mode==0?"invalid phrase":mode==2?"abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about":""};
            var rejected=await provider.SendTokenAsync(request);
            Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<ArgumentException>(rejected.Exception);
            Assert.IsFalse(provider.IsProviderActivated,"Invalid caller signer input must fail before activation.");
        }finally{Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task IndependentSenderCannotSilentlyUseServiceKey(bool native)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","unused","unused","unused","service.near","unused");
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest{FromTokenAddress=native?"NEAR":"owned-custom-token",
            FromWalletAddress="independent.near",ToWalletAddress="receiver.near",Amount=1m});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task UnrepresentableAmountFailsBeforeActivation(bool native)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","unused","unused","unused","unused","unused");
        var request=new SendWeb3TokenRequest{FromTokenAddress=native?"NEAR":"owned-custom-token",ToWalletAddress="receiver.near",
            Amount=native?0.0000000000000000000000001m:0.000000001m};
        var result=await provider.SendTokenAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
    }
}
