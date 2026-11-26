using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.ABI;

namespace NextGenSoftware.OASIS.API.Providers.SignProtocolOASIS.UnitTests;

[TestClass]
public sealed class SignProtocolRecordCodecTests
{
    private const string Sp = "0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512";
    private const string Key = "0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d";

    [TestMethod]
    public void RecordRoundTripsThroughAttestationData()
    {
        var data = SignProtocolKeyValueBackend.EncodeRecord("oasis/private/v1/ab", 7, "value é");
        var (key, seq, value) = SignProtocolKeyValueBackend.DecodeRecord(data, 1);
        Assert.AreEqual("oasis/private/v1/ab", key);
        Assert.AreEqual(7UL, seq);
        Assert.AreEqual("value é", value);
    }

    [TestMethod]
    public void NonCanonicalOrForeignDataIsRejected()
    {
        Assert.ThrowsException<InvalidDataException>(() => SignProtocolKeyValueBackend.DecodeRecord(new byte[] { 1, 2, 3 }, 1));
        Assert.ThrowsException<InvalidDataException>(() => SignProtocolKeyValueBackend.DecodeRecord(SignProtocolKeyValueBackend.EncodeRecord("k", 0, "v"), 1));
        var padded = SignProtocolKeyValueBackend.EncodeRecord("k", 1, "v").Concat(new byte[32]).ToArray();
        Assert.ThrowsException<InvalidDataException>(() => SignProtocolKeyValueBackend.DecodeRecord(padded, 1));
        var otherShape = new ABIEncode().GetABIEncoded(new ABIValue("uint256", 1), new ABIValue("uint256", 2), new ABIValue("uint256", 3));
        Assert.ThrowsException<InvalidDataException>(() => SignProtocolKeyValueBackend.DecodeRecord(otherShape, 1));
    }

    [TestMethod]
    public void IndexingKeyIsVersionedKeccakOfTheKey()
    {
        // keccak256("") is the well-known c5d2...a470.
        Assert.AreEqual("oasis-kv:v1:c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470", SignProtocolKeyValueBackend.IndexingKey(""));
        Assert.AreNotEqual(SignProtocolKeyValueBackend.IndexingKey("a"), SignProtocolKeyValueBackend.IndexingKey("b"));
    }

    [TestMethod]
    public void ProviderRequiresA32ByteRecordKeyAndValidConnection()
    {
        var connection = new SignProtocolConnection(new Uri("http://127.0.0.1:18545"), 31337, Sp, Key, 1, 1);
        Assert.ThrowsException<ArgumentException>(() => new SignProtocolOASIS(connection, new byte[16]));
        Assert.ThrowsException<ArgumentException>(() => new SignProtocolConnection(connection.RpcUrl, 31337, "not-an-address", Key, 1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new SignProtocolConnection(connection.RpcUrl, 0, Sp, Key, 1, 1));
        var provider = new SignProtocolOASIS(connection, new byte[32]);
        Assert.AreEqual("0x70997970C51812dc3A010C7d01b50e0d17dc79C8", provider.WalletAddress);
        Assert.AreEqual(1UL, provider.SchemaId);
        Assert.IsTrue(provider.GetHolonsNearMe(0, 0, 1).IsError);
    }
}
