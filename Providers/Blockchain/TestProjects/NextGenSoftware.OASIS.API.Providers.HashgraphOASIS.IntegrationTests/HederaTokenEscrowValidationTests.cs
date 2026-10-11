using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaTokenEscrowValidationTests
{
    [DataTestMethod]
    [DataRow("", 400000L, 100000000L)]
    [DataRow("0.0.0", 400000L, 100000000L)]
    [DataRow("0.0.2001", 0L, 100000000L)]
    [DataRow("0.0.2001", 9007199254740992L, 100000000L)]
    [DataRow("0.0.2001", 400000L, 0L)]
    public async Task InvalidDeploymentCannotReachWalletLookup(string contract, long gas, long fee)
    {
        var provider = Configured(contract, gas, fee);
        var locked = await provider.LockTokenAsync(new LockWeb3TokenRequest { LockedByAvatarId = Guid.NewGuid(), TokenAddress = "0.0.3001" });
        var unlocked = await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { UnlockedByAvatarId = Guid.NewGuid(), TokenAddress = "0.0.3001" });
        Assert.IsTrue(locked.IsError); Assert.IsNull(locked.Result);
        Assert.IsTrue(unlocked.IsError); Assert.IsNull(unlocked.Result);
        StringAssert.Contains(locked.Message, "fungible escrow contract");
    }

    [DataTestMethod]
    [DataRow("bad")]
    [DataRow("0.0.0")]
    [DataRow("0.0.3001/1")]
    public async Task InvalidTokenFailsBeforeWalletLookup(string token)
    {
        var provider = Configured();
        var result = await provider.LockTokenAsync(new LockWeb3TokenRequest { LockedByAvatarId = Guid.NewGuid(), TokenAddress = token });
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "HTS token address");
    }

    [TestMethod]
    public async Task NullAnonymousAndMissingConfigurationCannotReportSuccess()
    {
        foreach (var provider in new[] { Configured(), new HashgraphOASIS(network: "testnet") })
        {
            var results = new[] { await provider.LockTokenAsync(null!), await provider.UnlockTokenAsync(null!),
                await provider.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = "0.0.3001" }) };
            foreach (var result in results) { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); }
        }
    }

    private static HashgraphOASIS Configured(string contract = "0.0.2001", long gas = 400000, long fee = 100000000)
        => new(network: "testnet", tokenEscrowConfiguration: new HederaTokenEscrowConfiguration
        { ContractId = contract, RuntimeCodeHash = "0x" + new string('1', 64), GasLimit = gas, MaxFeeTinybars = fee });
}
