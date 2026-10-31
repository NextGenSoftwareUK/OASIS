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
    [Fact]
    public void EnergyFeeLimitDoesNotTruncateBandwidthOrOtherChargedFees()
    {
        var (tx,receipt,_)=Fixture();
        tx.RawData.FeeLimit=7;
        var id=ByteString.CopyFrom(SHA256.HashData(tx.RawData.ToByteArray()));receipt.Id=id;
        receipt.Fee=12;receipt.Receipt.EnergyFee=7;receipt.Receipt.NetFee=3;
        var mapped=Map(tx,receipt,id);
        Assert.Equal("12",mapped.GetProperty("netUnits").GetString());
        var components=mapped.GetProperty("components");
        Assert.Equal("7",components.GetProperty("energyFee").GetString());
        Assert.Equal("3",components.GetProperty("bandwidthFee").GetString());
        Assert.Equal("2",components.GetProperty("otherFee").GetString());
    }
    [Fact]
    public void PermissionUpdateFeeRetainsExactOriginalOwnerAndTotal()
    {
        var (tx,receipt,_) = Fixture();
        var owner=tx.RawData.Contract[0].Parameter.Unpack<TriggerSmartContract>().OwnerAddress;
        tx.RawData.Contract.Clear();tx.RawData.Contract.Add(new Transaction.Types.Contract{
            Type=Transaction.Types.Contract.Types.ContractType.AccountPermissionUpdateContract,
            Parameter=Any.Pack(new AccountPermissionUpdateContract{OwnerAddress=owner})});
        var id=ByteString.CopyFrom(SHA256.HashData(tx.RawData.ToByteArray()));receipt.Id=id;receipt.Receipt=null;
        var mapped=Map(tx,receipt,id);
        Assert.True(mapped.GetProperty("success").GetBoolean());
        Assert.Equal("9007199254740993",mapped.GetProperty("netUnits").GetString());
        Assert.Equal(NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(owner.ToByteArray()),mapped.GetProperty("payer").GetString());
    }
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
