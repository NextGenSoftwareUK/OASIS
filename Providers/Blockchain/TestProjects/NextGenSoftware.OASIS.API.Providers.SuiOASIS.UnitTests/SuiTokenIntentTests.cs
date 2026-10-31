using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass, DoNotParallelize]
public sealed class SuiTokenIntentTests
{
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(false, true)]
    [DataRow(true, false)] [DataRow(true, true)]
    public async Task SendPreservesOriginalIntentAcrossSignerAwait(bool native, bool synchronous)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "unlock-readback-failure.mjs"));
        try
        {
            using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", contractAddress: "0x1");
            var request = new SendWeb3TokenRequest { FromTokenAddress = native ? "0x2::sui::SUI" : "0x1::token::TOKEN",
                FromWalletAddress = "intent-owner", ToWalletAddress = "intent-recipient", Amount = 1.23m,
                OwnerPrivateKey = "test-only-explicit", OwnerPublicKey = "fixture-public" };
            NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse> result;
            if (synchronous) result = provider.SendToken(request);
            else
            {
                var pending = provider.SendTokenAsync(request);
                Assert.IsFalse(pending.IsCompleted, "Must exercise signer await.");
                request.FromWalletAddress = "changed"; request.ToWalletAddress = "changed";
                request.Amount = 99; request.MemoText = "unsupported"; request.FromTokenAddress = "changed";
                result = await pending;
            }
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(new string('1', 32), result.Result.TransactionResult);
            Assert.IsFalse(provider.IsProviderActivated);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE", previous); }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task UnsupportedMemoRejectedBeforeSignerLookup(bool native)
    {
        using var provider = new SuiOASIS(contractAddress: "0x1");
        var result = await provider.SendTokenAsync(new SendWeb3TokenRequest {
            FromTokenAddress = native ? "0x2::sui::SUI" : "0x1::token::TOKEN", Amount = 1, MemoText = "unsupported" });
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(ArgumentException));
        StringAssert.Contains(result.Message, "memo");
    }
}
