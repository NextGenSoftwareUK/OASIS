using System.Reflection;
using System.Security.Cryptography;
using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronStatusSafetyTests
{
    [Fact]
    public void ConfirmedReceiptDistinguishesFailureFromAmbiguousExecution()
    {
        var id = ByteString.CopyFrom(new byte[32]);
        var receipt = new TransactionInfo { Id = id, BlockNumber = 1 };
        var method = typeof(TronNativeTransferClient).GetMethod("MapConfirmedTransactionStatus", BindingFlags.NonPublic | BindingFlags.Static)!;
        object Status(bool contract) => method.Invoke(null, new object[] { receipt, id, contract })!;
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed, Status(false));
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Status(true)).InnerException);
        receipt.Receipt = new ResourceReceipt { Result = Transaction.Types.Result.Types.contractResult.Success };
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed, Status(true));
        receipt.Receipt.Result = Transaction.Types.Result.Types.contractResult.Revert;
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Failed, Status(true));
        receipt.Receipt = null;
        receipt.Result = (TransactionInfo.Types.code)1;
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Failed, Status(true));
        receipt.BlockNumber = 0;
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Status(true)).InnerException);
    }
    private static bool Exists(Transaction tx, ByteString id) => (bool)typeof(TronNativeTransferClient)
        .GetMethod("ValidateTransactionIdentity", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, new object[] { tx, id })!;

    [Fact]
    public void EmptyNodeResponseCannotProvePendingTransaction()
        => Assert.False(Exists(new Transaction(), ByteString.CopyFrom(new byte[32])));

    [Fact]
    public void TransactionIdentityMustMatchSerializedRawData()
    {
        var tx = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 123 } };
        tx.RawData.Contract.Add(new Transaction.Types.Contract());
        var id = ByteString.CopyFrom(SHA256.HashData(tx.RawData.ToByteArray()));
        Assert.True(Exists(tx, id));
        tx.RawData.Timestamp++;
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Exists(tx, id)).InnerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    public async Task InvalidHashCannotActivateProvider(string hash)
    {
        var provider = new TRONOASIS();
        Assert.True((await provider.GetTransactionStatusAsync(hash)).IsError);
        Assert.False(provider.IsProviderActivated);
    }
}
