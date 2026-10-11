using System.Reflection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletTokenSubmissionTests
{
    [Theory]
    [InlineData(false, "readback")]
    [InlineData(true, "readback")]
    [InlineData(false, "success")]
    [InlineData(true, "success")]
    [InlineData(false, "null")]
    [InlineData(true, "null")]
    [InlineData(false, "throw")]
    [InlineData(true, "throw")]
    public async Task AmbiguousOrFailedSubmissionIsNeverResent(bool asynchronous, string outcome)
    {
        var provider = new Mock<IOASISBlockchainStorageProvider>();
        provider.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SuiOASIS));
        var expected = new OASISResult<ITransactionResponse>(new TransactionResponse { TransactionResult = "original-native-digest" })
        { IsError = outcome == "readback", Message = "original native receipt/readback detail" };
        var calls = 0;
        OASISResult<ITransactionResponse> Submit()
        {
            calls++;
            if (outcome == "throw") throw new InvalidOperationException("original-native-digest needs reconciliation");
            return outcome == "null" ? null! : expected;
        }
        provider.Setup(p => p.SendToken(It.IsAny<ISendWeb3TokenRequest>())).Returns(Submit);
        provider.Setup(p => p.SendTokenAsync(It.IsAny<ISendWeb3TokenRequest>())).Returns(() => Task.FromResult(Submit()));
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        var providers = new ProviderManager(null, dna);
        Assert.True(providers.RegisterProvider(provider.Object));
        var manager = new WalletManager(null, dna, providers);
        var request = new SendWeb4TokenRequest
        {
            FromProvider = new EnumValue<ProviderType>(ProviderType.SuiOASIS),
            WaitTillTokenSent = true, AttemptToSendTokenEveryXSeconds = 0, WaitForTokenToSendInSeconds = 60,
            FromTokenAddress = "0x2::sui::SUI", FromWalletAddress = "sender", ToWalletAddress = "recipient", Amount = 1
        };
        var method = typeof(WalletManager).GetMethod(asynchronous ? "SendTokenInternalAsync" : "SendTokenInternal", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var invocation = method.Invoke(manager, new object[] { request });
        var actual = asynchronous ? await (Task<OASISResult<ITransactionResponse>>)invocation! : (OASISResult<ITransactionResponse>)invocation!;
        Assert.Equal(1, calls);
        if (outcome is "success" or "readback")
        {
            Assert.Same(expected, actual);
            Assert.Equal("original-native-digest", actual.Result.TransactionResult);
            Assert.Equal("original native receipt/readback detail", actual.Message);
            Assert.Equal(outcome == "readback", actual.IsError);
        }
        else
        {
            Assert.True(actual.IsError);
            Assert.Null(actual.Result);
            Assert.Contains("Reconcile", actual.Message);
        }
    }
}
