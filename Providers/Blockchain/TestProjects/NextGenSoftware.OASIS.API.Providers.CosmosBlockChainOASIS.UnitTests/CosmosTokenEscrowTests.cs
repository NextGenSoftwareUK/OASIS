using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass, DoNotParallelize]
public sealed class CosmosTokenEscrowTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(true, false)] [DataRow(false, true)] [DataRow(true, true)]
    public async Task ExplicitLockPreservesIntentAndOriginalReadbackFailure(bool synchronous, bool badReadback)
    {
        var bridge = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        var previousState = Environment.GetEnvironmentVariable("OASIS_COSMOS_ESCROW_FIXTURE_STATE");
        var state = Path.Combine(AppContext.BaseDirectory, "escrow-fixture-state.json");
        Assert.IsFalse(File.Exists(state), "Do not replace unresolved prior fixture evidence.");
        File.WriteAllText(state, JsonSerializer.Serialize(new { mode = badReadback ? "bad-readback" : "valid", submissions = 0, deposit = (object?)null }));
        Environment.SetEnvironmentVariable("OASIS_COSMOS_ESCROW_FIXTURE_STATE", state);
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "nft-signer.mjs"));
        var verified = false;
        try
        {
            using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", contractAddress: "fixture-contract",
                tokenEscrow: new("fixture-contract", new string('c', 64)));
            var request = new LockWeb3TokenRequest { LockedByAvatarId = Id, LockId = Id, Amount = 1.23m, TokenAddress = "TREE",
                EscrowContractAddress = "fixture-contract", FromWalletAddress = "fixture-owner", FromWalletPrivateKey = "test-only-explicit" };
            NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse> result;
            if (synchronous) result = provider.LockToken(request);
            else
            {
                var pending = provider.LockTokenAsync(request); Assert.IsFalse(pending.IsCompleted);
                request.Amount = 99; request.TokenAddress = "changed"; request.LockId = Guid.NewGuid();
                request.EscrowContractAddress = "changed"; request.FromWalletAddress = "changed";
                result = await pending;
            }
            if (badReadback)
            {
                Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
                var error = result.Exception as CosmosNativeMutationReconciliationException;
                Assert.IsNotNull(error); Assert.AreEqual(new string('a', 64), error.OriginalTransactionId);
                Assert.AreEqual(true, error.ExecutionSucceeded);
            }
            else
            {
                Assert.IsFalse(result.IsError, result.Message);
                var response = result.Result as CosmosIdBoundEscrowResponse; Assert.IsNotNull(response);
                Assert.AreEqual(Id, response.LockId); Assert.AreEqual(new string('a', 64), response.TransactionResult);
                Assert.AreEqual(1, response.NativeTransactions.Count);
            }
            using var observed = JsonDocument.Parse(File.ReadAllText(state));
            Assert.AreEqual(1, observed.RootElement.GetProperty("submissions").GetInt32());
            Assert.IsFalse(provider.IsProviderActivated); verified = true;
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", bridge);
            Environment.SetEnvironmentVariable("OASIS_COSMOS_ESCROW_FIXTURE_STATE", previousState);
            if (verified) File.Delete(state); // Preserve unexpected failures for inspection.
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ExplicitIdentityCannotDowngradeToIssuerFreeze(bool unlock)
    {
        using var provider = new CosmosBlockChainOASIS(contractAddress: "fixture-contract");
        var result = unlock ? await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { LockId = Id, TokenAddress = "TREE" })
            : await provider.LockTokenAsync(new LockWeb3TokenRequest { LockId = Id, TokenAddress = "TREE", Amount = 1 });
        Assert.IsTrue(result.IsError); Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
    }

    [DataTestMethod]
    [DataRow("", "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
    [DataRow("wrong-contract", "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")]
    [DataRow("fixture-contract", "CC")]
    public void InvalidReviewedConfigurationRejects(string address, string hash)
        => Assert.ThrowsException<ArgumentException>(() => new CosmosBlockChainOASIS(contractAddress: "fixture-contract", tokenEscrow: new(address, hash)));
}
