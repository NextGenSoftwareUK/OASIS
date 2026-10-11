using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNativeBridgeTests
{
    [DataTestMethod]
    [DataRow("bad", "1")]
    [DataRow("0.0.1234", "0")]
    [DataRow("0.0.1234", "-1")]
    [DataRow("0.0.1234", "9223372036854775808")]
    [DataRow("0.0.1234/1", "2")]
    [DataRow("0.0.1234", "")]
    public async Task NftBridgeRejectsInvalidOrConflictingSerialBeforeReadingSigner(string address, string serial)
    {
        var reads = 0;
        var provider = new HashgraphOASIS(network: "testnet", bridgePoolAccountId: "0.0.1002",
            bridgePoolSigningKey: () => { reads++; return "unused"; });
        var deposit = await provider.DepositNFTAsync(address, serial, "0.0.1001", "unverified-source-hash");
        var withdrawal = await provider.WithdrawNFTAsync(address, serial, "0.0.1001", "unused");
        Assert.IsTrue(deposit.IsError); Assert.IsNull(deposit.Result);
        Assert.IsTrue(withdrawal.IsError); Assert.IsNull(withdrawal.Result);
        Assert.AreEqual(0, reads);
    }

    [TestMethod]
    public async Task NftBridgeRequiresConfiguredPoolAndRealSigner()
    {
        foreach (var pool in new string?[] { null, "0.0.0", "0.0.1002" })
        {
            var provider = new HashgraphOASIS(network: "testnet", bridgePoolAccountId: pool);
            var deposit = await provider.DepositNFTAsync("0.0.1234", "1", "0.0.1001");
            var withdrawal = await provider.WithdrawNFTAsync("0.0.1234/1", "1", "0.0.1001", "");
            Assert.IsTrue(deposit.IsError); Assert.IsNull(deposit.Result);
            Assert.IsTrue(withdrawal.IsError); Assert.IsNull(withdrawal.Result);
        }
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("0.0.0")]
    [DataRow("not-an-account")]
    public async Task MissingOrInvalidPoolNeverInventsDestinationOrReadsPoolCredentials(string? pool)
    {
        var reads = 0;
        var provider = new HashgraphOASIS(network: "testnet", bridgePoolAccountId: pool,
            bridgePoolSigningKey: () => { reads++; return "unused"; });
        var withdrawal = await provider.WithdrawAsync(1m, "0.0.1001", "unused");
        var deposit = await provider.DepositAsync(1m, "0.0.1001");
        Assert.IsTrue(withdrawal.IsError); Assert.IsNull(withdrawal.Result);
        Assert.IsTrue(deposit.IsError); Assert.IsNull(deposit.Result);
        Assert.AreEqual(0, reads);
        StringAssert.Contains(deposit.Message, "bridge pool account");
    }

    [DataTestMethod]
    [DataRow("0")]
    [DataRow("-1")]
    [DataRow("0.000000001")]
    [DataRow("92233720368.54775808")]
    public async Task InvalidAmountsFailBeforePoolCredentialResolution(string value)
    {
        var reads = 0;
        var provider = new HashgraphOASIS(network: "testnet", bridgePoolAccountId: "0.0.1002",
            bridgePoolSigningKey: () => { reads++; return "unused"; });
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var deposit = await provider.DepositAsync(amount, "0.0.1001");
        var withdrawal = await provider.WithdrawAsync(amount, "0.0.1001", "unused");
        Assert.IsTrue(deposit.IsError); Assert.IsNull(deposit.Result);
        Assert.IsTrue(withdrawal.IsError); Assert.IsNull(withdrawal.Result);
        Assert.AreEqual(0, reads);
    }

    [TestMethod]
    public async Task PoolReleaseRequiresRealSignerAndDifferentExplicitRecipient()
    {
        var provider = new HashgraphOASIS(network: "testnet", bridgePoolAccountId: "0.0.1002");
        foreach (var recipient in new[] { "0.0.1002", "0.0.0", "bad", "0.0.1001" })
        {
            var deposit = await provider.DepositAsync(1m, recipient);
            Assert.IsTrue(deposit.IsError); Assert.IsNull(deposit.Result);
        }
        var withdrawal = await provider.WithdrawAsync(1m, "0.0.1001", "");
        Assert.IsTrue(withdrawal.IsError); Assert.IsNull(withdrawal.Result);
    }
}
