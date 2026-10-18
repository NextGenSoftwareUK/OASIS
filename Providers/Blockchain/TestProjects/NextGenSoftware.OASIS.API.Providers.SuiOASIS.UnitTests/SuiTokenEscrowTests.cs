using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass, DoNotParallelize]
public sealed class SuiTokenEscrowTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly string Escrow = "0x" + new string('2', 64);
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(false,true)] [DataRow(true,false)] [DataRow(true,true)]
    public async Task BalanceUsesSelectedCustodyOnlyWhenLocked(bool configured,bool locked)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",contractAddress:"0x1",tokenEscrow:configured?new(Escrow):null);
            var result=await provider.GetTokenBalanceAsync("fixture-owner","0x1::token::TOKEN",locked);
            Assert.IsFalse(result.IsError,result.Message);
            Assert.AreEqual((configured&&locked?123m:7m)/1000000000m,result.Result);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally{Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task CreationRejectsMissingAvatarOrAlreadyConfiguredRegistry(bool configured)
    {
        using var provider = new SuiOASIS(contractAddress:"0x1", tokenEscrow:configured?new(Escrow):null);
        var result = await provider.CreateTokenEscrowRegistryAsync(configured?Id:Guid.Empty);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(true, false)] [DataRow(false, true)] [DataRow(true, true)]
    public async Task ExplicitLockCapturesIntentAndRetainsOriginalInconsistentReply(bool synchronous, bool badReply)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "unlock-readback-failure.mjs"));
        try
        {
            using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", contractAddress: "0x1", tokenEscrow: new(Escrow));
            var request = new LockWeb3TokenRequest { LockedByAvatarId = Id, LockId = Id, Amount = 1.23m, TokenAddress = "0x1::token::TOKEN",
                EscrowContractAddress = Escrow, FromWalletAddress = "fixture-owner", FromWalletPrivateKey = badReply ? "test-only-bad-reply" : "test-only-explicit" };
            NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse> result;
            if (synchronous) result = provider.LockToken(request);
            else
            {
                var pending = provider.LockTokenAsync(request); Assert.IsFalse(pending.IsCompleted);
                request.Amount = 99; request.LockId = Guid.NewGuid(); request.FromWalletAddress = "changed";
                request.EscrowContractAddress = "changed"; request.TokenAddress = "changed";
                result = await pending;
            }
            if (badReply)
            {
                Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
                var error = result.Exception as SuiNativeMutationReconciliationException;
                Assert.IsNotNull(error); Assert.AreEqual(new string('1', 32), error.OriginalTransactionId);
                Assert.AreEqual(true, error.ExecutionSucceeded);
            }
            else
            {
                Assert.IsFalse(result.IsError, result.Message);
                var response = result.Result as SuiIdBoundEscrowResponse; Assert.IsNotNull(response);
                Assert.AreEqual(Id, response.LockId); Assert.AreEqual(new string('1', 32), response.TransactionResult);
                Assert.AreEqual(1, response.NativeTransactions.Count);
            }
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", previous); }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ExplicitIdentityCannotDowngradeToAggregate(bool unlock)
    {
        using var provider = new SuiOASIS(contractAddress: "0x1");
        var result = unlock ? await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = "0x1::token::TOKEN", LockId = Id })
            : await provider.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = "0x1::token::TOKEN", LockId = Id, Amount = 1 });
        Assert.IsTrue(result.IsError); Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
    }
    [DataTestMethod]
    [DataRow("")] [DataRow("0x2")] [DataRow("0xGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void InvalidExplicitConfigurationRejects(string escrow)
        => Assert.ThrowsException<ArgumentException>(() => new SuiOASIS(tokenEscrow: new(escrow)));
}
