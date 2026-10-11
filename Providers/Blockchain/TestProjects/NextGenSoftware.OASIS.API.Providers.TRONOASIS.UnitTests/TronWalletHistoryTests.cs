using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronWalletHistoryTests
{
    [Fact]
    public void NativeMappingPreservesHashAndExactSunWithoutFabricatingGuid()
    {
        var address = new byte[21]; address[0] = 0x41; address[20] = 7;
        var owner = ByteString.CopyFrom(address); address[20] = 8;
        var transfer = new TransferContract { OwnerAddress = owner, ToAddress = ByteString.CopyFrom(address), Amount = 9007199254740993L };
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = 1700000000000 } };
        transaction.RawData.Contract.Add(new Transaction.Types.Contract
            { Type = Transaction.Types.Contract.Types.ContractType.TransferContract, Parameter = Any.Pack(transfer) });
        var receipt = new TransactionInfo { Id = ByteString.CopyFrom(Enumerable.Repeat((byte)255, 32).ToArray()) };
        var method = typeof(TronNativeTransferClient).GetMethod("MapWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!;
        var mapped = (NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses.IWalletTransaction)method.Invoke(null, new object[] { transaction, receipt, owner })!;
        Assert.Equal(Guid.Empty, mapped.TransactionId);
        Assert.Equal(new string('f', 64), mapped.NativeTransactionId);
        Assert.Equal("9007199254740993", mapped.RawAmount);
        Assert.Equal("SUN", mapped.AmountUnit);
        Assert.Equal("TransferContract", mapped.NativeOperation);
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Enums.TransactionType.Debit, mapped.TransactionType);
        Assert.Equal(DateTimeKind.Utc, mapped.CreatedDate.Kind);
        using var json = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(mapped));
        Assert.Equal(new string('f', 64), json.RootElement.GetProperty("NativeTransactionId").GetString());
        Assert.Equal("9007199254740993", json.RootElement.GetProperty("RawAmount").GetString());
        Assert.Equal("SUN", json.RootElement.GetProperty("AmountUnit").GetString());
    }
}
