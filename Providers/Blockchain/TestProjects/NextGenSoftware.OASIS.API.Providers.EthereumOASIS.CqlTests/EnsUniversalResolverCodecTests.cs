using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.ENSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EnsUniversalResolverCodecTests
{
    [TestMethod]
    public void OfficialDnsExampleAndEthNamehashMatchKnownWireVectors()
    {
        Assert.AreEqual("026d79046e616d650365746800",
            Convert.ToHexString(EnsUniversalResolverCodec.DnsEncode("my.name.eth")).ToLowerInvariant());
        Assert.AreEqual("93cdeb708b7545dc668eb9280176169d1c33cfd8ed6f04690a0bcc88a93fc4ae",
            Convert.ToHexString(EnsUniversalResolverCodec.Namehash("eth")).ToLowerInvariant());
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("Nick.eth")]
    [DataRow("é.eth")]
    [DataRow("nick..eth")]
    [DataRow("nick.eth.")]
    [DataRow("-nick.eth")]
    [DataRow("nick-.eth")]
    [DataRow("nick_eth")]
    public void UnsupportedFormsAreRejectedWithoutNormalization(string name) =>
        Assert.ThrowsException<ArgumentException>(() => EnsUniversalResolverCodec.EncodeAddressCall(name));

    [TestMethod]
    public void OversizedLabelAndNameAreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => EnsUniversalResolverCodec.DnsEncode(new string('a', 64) + ".eth"));
        Assert.ThrowsException<ArgumentException>(() => EnsUniversalResolverCodec.DnsEncode(string.Join('.', Enumerable.Repeat(new string('a', 63), 4))));
    }

    [TestMethod]
    public void AddressCallUsesTwoIndependentDynamicTailsAndAddrSelector()
    {
        var bytes = Convert.FromHexString(EnsUniversalResolverCodec.EncodeAddressCall("nick.eth")[2..]);
        // Selector + 64-byte head + DNS tail (64) + addr tail (96).
        Assert.AreEqual(228, bytes.Length);
        Assert.AreEqual(64, bytes[35]);
        Assert.AreEqual(128, bytes[67]);
        Assert.AreEqual(10, bytes[99]);
        Assert.AreEqual(36, bytes[163]);
        CollectionAssert.AreEqual(Convert.FromHexString("3b3b57de"), bytes[164..168]);
        Assert.IsTrue(bytes[110..132].All(x => x == 0));
        Assert.IsTrue(bytes[200..].All(x => x == 0));
    }

    [TestMethod]
    public void CanonicalResultPreservesResolvedAddressAndResolverSeparately()
    {
        var result = EnsUniversalResolverCodec.DecodeAddressResult(Result());
        Assert.AreEqual("0x2222222222222222222222222222222222222222", result.Address);
        Assert.AreEqual("0x3333333333333333333333333333333333333333", result.Resolver);
    }

    [DataTestMethod]
    [DataRow(0)] // offset overflow/high bytes
    [DataRow(31)] // noncanonical offset
    [DataRow(32)] // resolver address padding
    [DataRow(64)] // length overflow/high bytes
    [DataRow(95)] // wrong record length
    [DataRow(96)] // resolved address padding
    public void MalformedAbiIsRejected(int position)
    {
        var bytes = Convert.FromHexString(Result()[2..]);
        bytes[position] ^= 1;
        Assert.ThrowsException<InvalidDataException>(() => EnsUniversalResolverCodec.DecodeAddressResult("0x" + Convert.ToHexString(bytes)));
    }

    [TestMethod]
    public void EmptyAddressesTruncationTrailingDataAndNonHexAreErrors()
    {
        foreach (var start in new[] { 44, 108 })
        {
            var bytes = Convert.FromHexString(Result()[2..]);
            Array.Clear(bytes, start, 20);
            Assert.ThrowsException<InvalidDataException>(() => EnsUniversalResolverCodec.DecodeAddressResult("0x" + Convert.ToHexString(bytes)));
        }
        foreach (var value in new[] { Result()[..^2], Result() + "00", "0X" + Result()[2..], Result()[..^1] + "g" })
            Assert.ThrowsException<InvalidDataException>(() => EnsUniversalResolverCodec.DecodeAddressResult(value));
    }

    private static string Result() => "0x" + new string('0', 62) + "40" +
        new string('0', 24) + new string('3', 40) + new string('0', 62) + "20" +
        new string('0', 24) + new string('2', 40);
}
