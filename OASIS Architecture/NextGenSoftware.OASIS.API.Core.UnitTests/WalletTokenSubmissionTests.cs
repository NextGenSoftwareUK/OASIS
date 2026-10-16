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
    public async Task UnlockPreservesOwnerAndSubmitsOnce(bool asynchronous, string outcome)
    {
        var provider = new Mock<IOASISBlockchainStorageProvider>();
        provider.As<IOASISIdBoundTokenEscrowProvider>();
        provider.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.HashgraphOASIS));
        var request = new UnlockWeb4TokenRequest
        {
            ProviderType = new EnumValue<ProviderType>(ProviderType.HashgraphOASIS), UnlockedByAvatarId = Guid.NewGuid(),
            Web3TokenId = Guid.NewGuid(), LockId = Guid.NewGuid(), EscrowContractAddress = "0.0.9001",
            TokenAddress = "0.0.3001", WaitTillTokenUnlocked = true, AttemptToUnlockEveryXSeconds = 0
        };
        var expected = new OASISResult<ITransactionResponse>(new TransactionResponse { TransactionResult = "original-unlock-id" })
        { IsError = outcome == "readback", Message = "original release readback detail" };
        var count = 0;
        IUnlockWeb3TokenRequest? submitted = null;
        OASISResult<ITransactionResponse> Submit(IUnlockWeb3TokenRequest projected)
        {
            count++;
            submitted = projected;
            if (outcome == "throw") throw new InvalidOperationException("original-unlock-id needs reconciliation");
            return outcome == "null" ? null! : expected;
        }
        provider.Setup(p => p.UnlockToken(It.IsAny<IUnlockWeb3TokenRequest>())).Returns((IUnlockWeb3TokenRequest r) => Submit(r));
        provider.Setup(p => p.UnlockTokenAsync(It.IsAny<IUnlockWeb3TokenRequest>())).Returns((IUnlockWeb3TokenRequest r) => Task.FromResult(Submit(r)));
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        var providers = new ProviderManager(null, dna); Assert.True(providers.RegisterProvider(provider.Object));
        var manager = new WalletManager(null, dna, providers);
        var actual = asynchronous ? await manager.UnlockTokenAsync(request) : manager.UnlockToken(request);
        Assert.Equal(1, count);
        Assert.NotNull(submitted);
        Assert.Equal(request.UnlockedByAvatarId, submitted.UnlockedByAvatarId);
        Assert.Equal(request.Web3TokenId, submitted.Web3TokenId); Assert.Equal(request.TokenAddress, submitted.TokenAddress);
        Assert.Equal(request.LockId, submitted.LockId);
        Assert.Equal(request.EscrowContractAddress, submitted.EscrowContractAddress);
        if (outcome is "success" or "readback")
        {
            Assert.Same(expected, actual); Assert.Equal("original release readback detail", actual.Message);
        }
        else { Assert.True(actual.IsError); Assert.Null(actual.Result); Assert.Contains("Reconcile", actual.Message); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LockPreservesAvatarAndSignerWithoutResubmittingReadbackFailure(bool asynchronous)
    {
        var provider = new Mock<IOASISBlockchainStorageProvider>();
        provider.As<IOASISIdBoundTokenEscrowProvider>();
        provider.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.HashgraphOASIS));
        var request = new LockWeb4TokenRequest
        {
            ProviderType = new EnumValue<ProviderType>(ProviderType.HashgraphOASIS), LockedByAvatarId = Guid.NewGuid(),
            Web3TokenId = Guid.NewGuid(), LockId = Guid.NewGuid(), Amount = 12.345678m, EscrowContractAddress = "0.0.9001",
            TokenAddress = "0.0.3001", FromWalletAddress = "0.0.1001",
            FromWalletPrivateKey = "test-only-key", WaitTillTokenLocked = true, AttemptToLockEveryXSeconds = 0
        };
        var expected = new OASISResult<ITransactionResponse>(new TransactionResponse { TransactionResult = "original-lock-id" })
        { IsError = true, Message = "confirmed approval; mutation readback failed" };
        var count = 0;
        ILockWeb3TokenRequest? submitted = null;
        OASISResult<ITransactionResponse> Submit(ILockWeb3TokenRequest projected)
        {
            count++;
            submitted = projected;
            return expected;
        }
        provider.Setup(p => p.LockToken(It.IsAny<ILockWeb3TokenRequest>())).Returns((ILockWeb3TokenRequest r) => Submit(r));
        provider.Setup(p => p.LockTokenAsync(It.IsAny<ILockWeb3TokenRequest>())).Returns((ILockWeb3TokenRequest r) => Task.FromResult(Submit(r)));
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        var providers = new ProviderManager(null, dna); Assert.True(providers.RegisterProvider(provider.Object));
        var manager = new WalletManager(null, dna, providers);
        var actual = asynchronous ? await manager.LockTokenAsync(request) : manager.LockToken(request);
        Assert.NotNull(submitted);
        Assert.Equal(request.LockedByAvatarId, submitted.LockedByAvatarId);
        Assert.Equal(request.FromWalletAddress, submitted.FromWalletAddress);
        Assert.Equal(request.FromWalletPrivateKey, submitted.FromWalletPrivateKey);
        Assert.Equal(request.Web3TokenId, submitted.Web3TokenId);
        Assert.Equal(request.TokenAddress, submitted.TokenAddress);
        Assert.Equal(request.LockId, submitted.LockId);
        Assert.Equal(request.Amount, submitted.Amount);
        Assert.Equal(request.EscrowContractAddress, submitted.EscrowContractAddress);
        Assert.Equal(1, count); Assert.Same(expected, actual);
        Assert.Equal("confirmed approval; mutation readback failed", actual.Message);
    }

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
