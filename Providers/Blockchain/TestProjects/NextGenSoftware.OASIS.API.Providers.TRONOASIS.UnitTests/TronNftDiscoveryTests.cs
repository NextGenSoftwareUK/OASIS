using System.Reflection;
using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftDiscoveryTests
{
    private static readonly MethodInfo Read = typeof(TronNativeTransferClient).GetMethod("NftCollectionsFromReceipt", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static ByteString Owner()
    {
        var bytes = new byte[21]; bytes[0] = 0x41; bytes[20] = 7;
        return ByteString.CopyFrom(bytes);
    }
    private static TransactionInfo.Types.Log Transfer(int topics = 4)
    {
        var log = new TransactionInfo.Types.Log { Address = ByteString.CopyFrom(new byte[20]) };
        log.Topics.Add(ByteString.CopyFrom(Convert.FromHexString(new Nethereum.Util.Sha3Keccack().CalculateHash("Transfer(address,address,uint256)"))));
        log.Topics.Add(ByteString.CopyFrom(new byte[32]));
        var recipient = new byte[32]; recipient[31] = 7;
        log.Topics.Add(ByteString.CopyFrom(recipient));
        if (topics == 4) log.Topics.Add(ByteString.CopyFrom(Enumerable.Repeat((byte)255, 32).ToArray()));
        return log;
    }
    private static IReadOnlyList<string> Collections(TransactionInfo receipt) =>
        (IReadOnlyList<string>)Read.Invoke(null, new object[] { receipt, Owner() })!;

    [Fact]
    public void NativeNftLogsDeduplicateCollectionsWithoutIndexerMetadata()
    {
        var receipt = new TransactionInfo(); receipt.Log.Add(Transfer()); receipt.Log.Add(Transfer());
        var bytes = new byte[21]; bytes[0] = 0x41;
        Assert.Equal(NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(bytes), Assert.Single(Collections(receipt)));
    }

    [Fact]
    public void FungibleTransferIsNotAnNftEvenWithMatchingOwner()
    {
        var receipt = new TransactionInfo(); receipt.Log.Add(Transfer(3));
        Assert.Empty(Collections(receipt));
    }

    [Fact]
    public void UnrelatedOwnerIsNotIncluded()
    {
        var log = Transfer(); log.Topics[2] = ByteString.CopyFrom(new byte[32]);
        var receipt = new TransactionInfo(); receipt.Log.Add(log);
        Assert.Empty(Collections(receipt));
    }

    [Fact]
    public void NoncanonicalAddressPaddingCannotBecomeAnInventoryEntry()
    {
        var log = Transfer(); var bytes = log.Topics[2].ToByteArray(); bytes[0] = 1;
        log.Topics[2] = ByteString.CopyFrom(bytes);
        var receipt = new TransactionInfo(); receipt.Log.Add(log);
        var error = Assert.Throws<TargetInvocationException>(() => Collections(receipt));
        Assert.IsType<InvalidOperationException>(error.InnerException);
    }

    [Fact]
    public async Task PrecancelledDiscoveryDoesNotAccessNetwork()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<OperationCanceledException>(() => runtime.NativeTransfers.DiscoverNftCollectionsAsync(
            "unused", Array.Empty<string>(), new CancellationToken(true)));
    }
}
