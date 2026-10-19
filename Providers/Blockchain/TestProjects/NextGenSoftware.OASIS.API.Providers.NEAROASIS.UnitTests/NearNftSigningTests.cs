using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearNftSigningTests
{
    [TestMethod]
    public async Task SuppliedPublicKeyMismatchCannotReachTransfer()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-fixture-key");
            var result=await provider.SendNFTAsync(new SendWeb3NFTRequest{TokenId="owned-id",ToWalletAddress="receiver.near",OwnerPrivateKey="fixture-private",OwnerPublicKey="mismatching-public-key"});
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);}
    }

    [TestMethod]
    public async Task IndependentSenderWithoutKeyIsRejectedBeforeActivation()
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-fixture-key");
        var result=await provider.SendNFTAsync(new SendWeb3NFTRequest{TokenId="owned-id",FromWalletAddress="independent.near",ToWalletAddress="receiver.near"});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task ConflictingBurnIdentitiesFailBeforeActivation()
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-fixture-key");
        var result=await provider.BurnNFTAsync(new BurnWeb3NFTRequest{TokenId="one-id",NFTTokenAddress="different-id",OwnerPublicKey="",OwnerPrivateKey="",OwnerSeedPhrase=""});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RejectedOwnershipOrDeletionAfterReceiptRetainsOriginalWithoutReplay(bool burn)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-fixture-key");
            Exception? error;
            if(burn){var result=await provider.BurnNFTAsync(new BurnWeb3NFTRequest{TokenId="owned-id",OwnerPublicKey="",OwnerPrivateKey="",OwnerSeedPhrase=""});Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);error=result.Exception;}
            else {var result=await provider.SendNFTAsync(new SendWeb3NFTRequest{TokenId="owned-id",ToWalletAddress="receiver.near"});Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);error=result.Exception;}
            var followup=error as NativeTransactionFollowupException;Assert.IsNotNull(followup);
            Assert.AreEqual("controlled-original-nft-transaction",followup.OriginalTransactionId);
            Assert.IsInstanceOfType(followup.InnerException,typeof(System.IO.InvalidDataException));
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));
            CollectionAssert.Contains(NativeMutationReconciliation.OriginalTransactionIds(error).ToArray(),"controlled-original-nft-transaction");
        }
        finally {Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);}
    }

    [TestMethod]
    public async Task MissingNftFailsBeforeBurnMutation()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE");var mode=Environment.GetEnvironmentVariable("OASIS_NEAR_NFT_TEST_MODE");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-followup.mjs"));
            Environment.SetEnvironmentVariable("OASIS_NEAR_NFT_TEST_MODE","missing");
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-fixture-key");
            var result=await provider.BurnNFTAsync(new BurnWeb3NFTRequest{TokenId="owned-id",OwnerPublicKey="",OwnerPrivateKey="",OwnerSeedPhrase=""});
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(KeyNotFoundException));Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_NEAR_SDK_BRIDGE",previous);Environment.SetEnvironmentVariable("OASIS_NEAR_NFT_TEST_MODE",mode);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task UnsupportedSeedCannotBeIgnoredInFavorOfServiceSigner(bool burn)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","service.near","controlled-fixture-key");
        if(burn){var result=await provider.BurnNFTAsync(new BurnWeb3NFTRequest{TokenId="owned-id",OwnerPublicKey="",OwnerPrivateKey="",OwnerSeedPhrase="unsupported mnemonic"});Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));}
        else {var result=await provider.SendNFTAsync(new SendWeb3NFTRequest{TokenId="owned-id",ToWalletAddress="receiver.near",OwnerSeedPhrase="unsupported mnemonic"});Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));}
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
