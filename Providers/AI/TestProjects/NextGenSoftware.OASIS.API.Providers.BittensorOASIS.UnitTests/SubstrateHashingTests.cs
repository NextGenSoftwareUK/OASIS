using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.BittensorOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BittensorOASIS.UnitTests;

[TestClass]
public sealed class SubstrateHashingTests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    [TestMethod]
    public void Blake2b512MatchesRfc7693()
        => Assert.AreEqual("ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d17d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923",
            Hex(SubstrateHashing.Blake2b(Encoding.ASCII.GetBytes("abc"), 64)));

    [TestMethod]
    public void Blake2bHandlesEmptyAndMultiBlockInput()
    {
        Assert.AreEqual("786a02f742015903c6c6fd852552d272912f4740e15847618a86e217f71f5419d25e1031afee585313896444934eb04b903a685b1448b755d56f701afe9be2ce",
            Hex(SubstrateHashing.Blake2b(Array.Empty<byte>(), 64)));
        var block = new byte[256];
        for (var i = 0; i < block.Length; i++) block[i] = (byte)i;
        // Exactly two blocks: the second must carry the final flag.
        Assert.AreEqual(64, SubstrateHashing.Blake2b(block, 64).Length);
        Assert.AreNotEqual(Hex(SubstrateHashing.Blake2b(block, 64)), Hex(SubstrateHashing.Blake2b(block.AsSpan(0, 255), 64)));
    }

    [TestMethod]
    public void XxHash64MatchesReference()
        => Assert.AreEqual(0xEF46DB3751D8E999UL, SubstrateHashing.XxHash64(ReadOnlySpan<byte>.Empty, 0));

    [TestMethod]
    public void Twox128MatchesSubstrateStoragePrefixes()
    {
        Assert.AreEqual("26aa394eea5630e07c48ae0c9558cef7", Hex(SubstrateHashing.Twox128("System")));
        Assert.AreEqual("b99d880ec681799c0cf30e8886371da9", Hex(SubstrateHashing.Twox128("Account")));
    }

    [TestMethod]
    public void Ss58DecodesAliceAndRejectsABadChecksum()
    {
        Assert.AreEqual("d43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d",
            Hex(SubstrateHashing.DecodeSs58("5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQY")));
        Assert.ThrowsException<FormatException>(() => SubstrateHashing.DecodeSs58("5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQZ"));
    }
}
