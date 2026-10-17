using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronEscrowSafetyTests
{
    [Theory]
    [InlineData("amount")] [InlineData("negative")]
    [InlineData("lock-id")] [InlineData("escrow")]
    public async Task LockCannotSilentlyIgnoreRequestedDepositSemantics(string mode)
    {
        var provider = new TRONOASIS(escrowContractAddress: "configured");
        var request = new Core.Objects.Wallet.Requests.LockWeb3TokenRequest {
            TokenAddress = "token", FromWalletAddress = "owner", FromWalletPrivateKey = "unused",
            Amount = mode == "amount" ? 1m : mode == "negative" ? -1m : 0m,
            LockId = mode == "lock-id" ? Guid.NewGuid() : Guid.Empty,
            EscrowContractAddress = mode == "escrow" ? "different" : "configured" };
        var result = await provider.LockTokenAsync(request);
        Assert.True(result.IsError); Assert.IsType<ArgumentException>(result.Exception);
        Assert.Null(result.Result); Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnlockCannotIgnoreDepositIdOrConfiguredEscrow(bool wrongEscrow)
    {
        var provider = new TRONOASIS(escrowContractAddress: "configured");
        var result = await provider.UnlockTokenAsync(new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest {
            UnlockedByAvatarId = Guid.NewGuid(), TokenAddress = "token",
            LockId = wrongEscrow ? Guid.Empty : Guid.NewGuid(),
            EscrowContractAddress = wrongEscrow ? "different" : "configured" });
        Assert.True(result.IsError); Assert.IsType<ArgumentException>(result.Exception);
        Assert.Null(result.Result); Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task LockRequiresSeparateEscrowConfigurationBeforeActivation()
    {
        var provider = new TRONOASIS(contractAddress: "not-an-escrow");
        var result = await provider.LockTokenAsync(new Core.Objects.Wallet.Requests.LockWeb3TokenRequest
        {
            TokenAddress = "token", FromWalletAddress = "owner", FromWalletPrivateKey = "key"
        });
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
        Assert.Contains("EscrowContractAddress", result.Message);
    }

    [Fact]
    public async Task LockCannotSignAsAnotherDepositor()
    {
        await TronPermissionRpcTests.WithDefaultOwnerAuthority(async runtime => {
        var other = new TronNet.TronECKey(new string('2', 64), TronNet.TronNetwork.MainNet).GetPublicAddress();
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(() => runtime.NativeTransfers.LockEscrowAsync(
            other, other, other, new string('1', 64)));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderUnlockCannotInventPoolOrDepositor(bool avatarProvided)
    {
        var provider = new TRONOASIS(contractAddress: "not-an-escrow");
        var result = await provider.UnlockTokenAsync(new Core.Objects.Wallet.Requests.UnlockWeb3TokenRequest
        {
            UnlockedByAvatarId = avatarProvided ? Guid.NewGuid() : Guid.Empty,
            TokenAddress = "token"
        });
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
        Assert.Contains(avatarProvided ? "EscrowContractAddress" : "UnlockedByAvatarId", result.Message);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void EscrowMustReturnTheExactRecordedDeposit(int returned, bool valid)
    {
        var receipt = new TransactionInfo();
        var bytes = new byte[32]; bytes[31] = (byte)returned;
        receipt.ContractResult.Add(ByteString.CopyFrom(bytes));
        var method = typeof(TronNativeTransferClient).GetMethod("ValidateContractAmount", BindingFlags.NonPublic | BindingFlags.Static)!;
        if (valid) method.Invoke(null, new object[] { receipt, BigInteger.One });
        else Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object[] { receipt, BigInteger.One })).InnerException);
    }

    [Fact]
    public async Task UnlockCannotSignAsAnotherDepositor()
    {
        await TronPermissionRpcTests.WithDefaultOwnerAuthority(async runtime => {
        var other = new TronNet.TronECKey(new string('2', 64), TronNet.TronNetwork.MainNet).GetPublicAddress();
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(() => runtime.NativeTransfers.UnlockEscrowAsync(
            other, other, other, new string('1', 64)));
        });
    }
}
