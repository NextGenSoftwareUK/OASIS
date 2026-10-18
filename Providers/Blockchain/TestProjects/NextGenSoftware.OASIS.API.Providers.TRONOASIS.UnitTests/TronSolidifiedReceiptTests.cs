using System.Reflection;
using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronSolidifiedReceiptTests
{
    private static readonly ByteString Id = ByteString.CopyFrom(new byte[32]);
    private static void Validate(TransactionInfo receipt, bool contract = false, bool transfer = false, bool mint = false)
        => typeof(TronNativeTransferClient).GetMethod("ValidateSolidifiedReceipt", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { receipt, Id, contract, transfer, mint });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void WrongIdentityMissingBlockOrFailedNativeReceiptCannotBeSuccess(int mode)
    {
        var receipt = new TransactionInfo { Id = Id, BlockNumber = 1 };
        if (mode == 0) receipt.Id = ByteString.CopyFrom(new byte[] { 7 });
        if (mode == 1) receipt.BlockNumber = 0;
        if (mode == 2) receipt.Result = (TransactionInfo.Types.code)1;
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Validate(receipt)).InnerException);
    }

    [Fact]
    public void ConfirmedNativeReceiptDoesNotRequireSmartContractReturn()
        => Validate(new TransactionInfo { Id = Id, BlockNumber = 1 });

    [Fact]
    public void DefaultOrMissingVmReceiptCannotConfirmContractExecution()
    {
        var receipt = new TransactionInfo { Id = Id, BlockNumber = 1 };
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Validate(receipt, contract: true)).InnerException);
        receipt.Receipt = new ResourceReceipt();
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Validate(receipt, contract: true)).InnerException);
    }
}
