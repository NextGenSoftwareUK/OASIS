using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftEscrowValidationTests
{
    [DataTestMethod]
    [DataRow("", 400000L, 100000000L)]
    [DataRow("0.0.0", 400000L, 100000000L)]
    [DataRow("0.0.2001", 0L, 100000000L)]
    [DataRow("0.0.2001", 9007199254740992L, 100000000L)]
    [DataRow("0.0.2001", 400000L, 0L)]
    public async Task EscrowRequiresExplicitContractGasAndFeeBeforeWalletLookup(string contract, long gas, long fee)
    {
        var provider = new HashgraphOASIS(network: "testnet", nftEscrowContractId: contract,
            nftEscrowGasLimit: gas, nftEscrowMaxFeeTinybars: fee);
        var avatar = Guid.Parse("354f54c3-67d2-44d8-a53d-6a6e616c2da2");
        var locked = await provider.LockNFTAsync(new LockWeb3NFTRequest { LockedByAvatarId = avatar, NFTTokenAddress = "0.0.1234/1" });
        var unlocked = await provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { UnlockedByAvatarId = avatar, NFTTokenAddress = "0.0.1234/1" });
        Assert.IsTrue(locked.IsError); Assert.IsNull(locked.Result);
        Assert.IsTrue(unlocked.IsError); Assert.IsNull(unlocked.Result);
        StringAssert.Contains(locked.Message, "escrow contract");
    }

    [DataTestMethod]
    [DataRow("bad")]
    [DataRow("0.0.1234")]
    [DataRow("0.0.1234/0")]
    [DataRow("0.0.1234/9223372036854775808")]
    public async Task InvalidAssetCannotReachWalletLookupOrBecomeSuccessfulLock(string address)
    {
        var provider = Configured();
        var avatar = Guid.Parse("354f54c3-67d2-44d8-a53d-6a6e616c2da2");
        var locked = await provider.LockNFTAsync(new LockWeb3NFTRequest { LockedByAvatarId = avatar, NFTTokenAddress = address });
        var unlocked = await provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { UnlockedByAvatarId = avatar, NFTTokenAddress = address });
        Assert.IsTrue(locked.IsError); Assert.IsNull(locked.Result);
        Assert.IsTrue(unlocked.IsError); Assert.IsNull(unlocked.Result);
        StringAssert.Contains(locked.Message, "NFT address");
    }

    [TestMethod]
    public async Task NullOrAnonymousRequestCannotResolveSigningWallet()
    {
        var provider = Configured();
        var requests = new[] {
            await provider.LockNFTAsync(null!), await provider.UnlockNFTAsync(null!),
            await provider.LockNFTAsync(new LockWeb3NFTRequest { NFTTokenAddress = "0.0.1234/1" }),
            await provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress = "0.0.1234/1" }) };
        foreach (var result in requests) { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); }
    }

    private static HashgraphOASIS Configured() => new(network: "testnet", nftEscrowContractId: "0.0.2001",
        nftEscrowGasLimit: 400000, nftEscrowMaxFeeTinybars: 100000000);
}
