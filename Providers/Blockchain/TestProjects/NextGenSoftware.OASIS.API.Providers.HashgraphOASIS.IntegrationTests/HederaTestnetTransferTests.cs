using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaTestnetTransferTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Funded testnet setting {name} is required; no skipped or mock evidence.");

    [TestMethod]
    public async Task FundedTestnetTransferHasSuccessfulConsensusReceipt()
    {
        var account = Required("OASIS_HEDERA_TEST_ACCOUNT_ID");
        var recipient = Required("OASIS_HEDERA_TEST_RECIPIENT_ID");
        var privateKey = Required("OASIS_HEDERA_TEST_PRIVATE_KEY");
        Assert.AreNotEqual(account, recipient);
        var provider = new HashgraphOASIS("https://testnet.mirrornode.hedera.com", "testnet", "296");
        var sent = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = account, ToWalletAddress = recipient, Amount = 0.00000001m,
            OwnerPrivateKey = privateKey, MemoText = "OASIS funded testnet consensus verification"
        });
        Assert.IsFalse(sent.IsError, sent.Message);
        Assert.IsNotNull(sent.Result);
        StringAssert.StartsWith(sent.Result.TransactionResult, account + "@");
        var status = await provider.GetTransactionStatusAsync(sent.Result.TransactionResult);
        Assert.IsFalse(status.IsError, status.Message);
        Assert.AreEqual(BridgeTransactionStatus.Completed, status.Result);
    }
}
