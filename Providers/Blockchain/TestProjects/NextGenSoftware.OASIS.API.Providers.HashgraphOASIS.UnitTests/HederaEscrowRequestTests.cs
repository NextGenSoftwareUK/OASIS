using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public sealed class HederaEscrowRequestTests
{
    [DataTestMethod]
    [DataRow("amount")] [DataRow("negative")] [DataRow("lock-id")] [DataRow("escrow")]
    [DataRow("unlock-id")] [DataRow("unlock-escrow")]
    public async Task AggregateEscrowCannotIgnoreRequestedDepositSemantics(string mode)
    {
        var provider = new HashgraphOASIS(tokenEscrowConfiguration: new HederaTokenEscrowConfiguration {
            ContractId = "0.0.2001", RuntimeCodeHash = "0x" + new string('1',64), GasLimit = 400000, MaxFeeTinybars = 100000000 });
        var avatar = Guid.NewGuid();
        var result = mode.StartsWith("unlock",StringComparison.Ordinal)
            ? await provider.UnlockTokenAsync(new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest {
                UnlockedByAvatarId = avatar, TokenAddress = "0.0.3001",
                LockId = mode == "unlock-id" ? Guid.NewGuid() : Guid.Empty,
                EscrowContractAddress = mode == "unlock-escrow" ? "0.0.9999" : "0.0.2001" })
            : await provider.LockTokenAsync(new Core.Objects.Wallet.Requests.LockWeb3TokenRequest {
                LockedByAvatarId = avatar, TokenAddress = "0.0.3001", Amount = mode == "amount" ? 1 : mode == "negative" ? -1 : 0,
                LockId = mode == "lock-id" ? Guid.NewGuid() : Guid.Empty,
                EscrowContractAddress = mode == "escrow" ? "0.0.9999" : "0.0.2001" });
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException)); Assert.IsFalse(provider.IsProviderActivated);
    }
}
