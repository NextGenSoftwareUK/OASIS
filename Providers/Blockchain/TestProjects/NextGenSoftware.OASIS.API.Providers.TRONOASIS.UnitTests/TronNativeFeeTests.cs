using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNativeFeeTests
{
    private static (Transaction Tx, TransactionInfo Receipt, ByteString Id) Fixture()
    {
        var address = new byte[21]; address[0] = 0x41; address[20] = 1;
        var tx = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        tx.RawData.Contract.Add(new Transaction.Types.Contract { Type = Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,
            Parameter = Any.Pack(new TriggerSmartContract { OwnerAddress = ByteString.CopyFrom(address), ContractAddress = ByteString.CopyFrom(address) }) });
        var id = ByteString.CopyFrom(SHA256.HashData(tx.RawData.ToByteArray()));
        return (tx, new TransactionInfo { Id = id, BlockNumber = 42, BlockTimeStamp = 1700000001000, Fee = 9007199254740993,
            Receipt = new ResourceReceipt { Result = Transaction.Types.Result.Types.contractResult.Success, EnergyFee = 7, NetFee = 3 } }, id);
    }
    private static JsonElement Map(Transaction tx, TransactionInfo receipt, ByteString id)
        => (JsonElement)typeof(TronNativeTransferClient).GetMethod("MapNativeFeeReceipt", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { tx, receipt, id, new string('a', 64) })!;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactTotalFeeAndChargedVmFailureAreRetained(bool failed)
    {
        var (tx, receipt, id) = Fixture();
        if (failed) receipt.Receipt.Result = Transaction.Types.Result.Types.contractResult.Revert;
        var mapped = Map(tx, receipt, id);
        Assert.Equal(!failed, mapped.GetProperty("success").GetBoolean());
        Assert.Equal("9007199254740993", mapped.GetProperty("netUnits").GetString());
        Assert.Equal("9007199254740983", mapped.GetProperty("components").GetProperty("otherFee").GetString());
        Assert.Equal("SUN", mapped.GetProperty("unit").GetString());
        Assert.StartsWith("T", mapped.GetProperty("payer").GetString());
    }

    [Theory]
    [InlineData("negative")]
    [InlineData("components")]
    [InlineData("identity")]
    [InlineData("unknown")]
    public void InvalidFeeOrUnknownExecutionCannotBecomeZeroSuccess(string reason)
    {
        var (tx, receipt, id) = Fixture();
        if (reason == "negative") receipt.Fee = -1;
        if (reason == "components") receipt.Fee = 9;
        if (reason == "identity") receipt.Id = ByteString.CopyFrom(new byte[32]);
        if (reason == "unknown") receipt.Receipt.Result = Transaction.Types.Result.Types.contractResult.Default;
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Map(tx, receipt, id)).InnerException);
    }

    [Fact]
    public async Task InvalidFeeHashDoesNotActivateStorage()
    {
        var provider = new TRONOASIS();
        Assert.True((await provider.GetNativeFeeReceiptAsync("not-a-hash")).IsError);
        Assert.False(provider.IsProviderActivated);
    }
}
