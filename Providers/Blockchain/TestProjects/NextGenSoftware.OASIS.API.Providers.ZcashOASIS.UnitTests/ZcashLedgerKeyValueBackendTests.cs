using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.UnitTests;

[TestClass]
public sealed class ZcashLedgerKeyValueBackendTests
{
    [TestMethod]
    public void RawCidMatchesKuboForAKnownBlock()
    {
        // CIDv1, raw codec, sha2-256 of the empty block: the value `ipfs add --cid-version=1 --raw-leaves` assigns it.
        Assert.AreEqual("bafkreihdwdcefgh4dqkjv67uzcmw7ojee6xedzdetojuzjevtenxquvyku",
            ZcashLedgerKeyValueBackend.RawCid(SHA256.HashData(Array.Empty<byte>())));
    }

    [TestMethod]
    public void RawCidIsLowercaseBase32WithoutPadding()
    {
        var cid = ZcashLedgerKeyValueBackend.RawCid(SHA256.HashData("record"u8.ToArray()));
        Assert.AreEqual('b', cid[0]);
        Assert.AreEqual(59, cid.Length); // 1 multibase prefix + ceil(36 bytes * 8 / 5)
        Assert.IsTrue(cid.Skip(1).All(c => c is >= 'a' and <= 'z' or >= '2' and <= '7'));
    }

    [TestMethod]
    public void ReadsADirectPushOpReturn()
    {
        byte[] payload = [.. "OASZK1D"u8.ToArray(), .. new byte[32]];
        byte[] script = [0x6a, (byte)payload.Length, .. payload];
        CollectionAssert.AreEqual(payload, ZcashLedgerKeyValueBackend.NullDataPayload(script));
    }

    [TestMethod]
    public void ReadsAPushData1OpReturn()
    {
        var payload = new byte[80];
        byte[] script = [0x6a, 0x4c, (byte)payload.Length, .. payload];
        CollectionAssert.AreEqual(payload, ZcashLedgerKeyValueBackend.NullDataPayload(script));
    }

    [TestMethod]
    public void RejectsNonOpReturnAndMalformedPushes()
    {
        Assert.IsNull(ZcashLedgerKeyValueBackend.NullDataPayload([0x76, 0xa9, 0x14]));
        Assert.IsNull(ZcashLedgerKeyValueBackend.NullDataPayload([0x6a, 0x05, 0x01, 0x02])); // push longer than script
        Assert.IsNull(ZcashLedgerKeyValueBackend.NullDataPayload([0x6a, 0x01, 0x01, 0x02])); // trailing bytes
        Assert.IsNull(ZcashLedgerKeyValueBackend.NullDataPayload([0x6a]));
    }
}
