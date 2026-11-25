using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS.UnitTests
{
    [TestClass]
    public class AztecLedgerKeyValueBackendTests
    {
        [TestMethod]
        public void RecordIdIsTheFirst31BytesOfTheKeySha256()
        {
            var expected = "0x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("oasis/holon/abc")), 0, 31).ToLowerInvariant();
            Assert.AreEqual(expected, AztecLedgerKeyValueBackend.RecordId("oasis/holon/abc"));
            Assert.AreEqual(2 + 62, expected.Length); // 31 bytes: always below the BN254 field modulus
        }

        [TestMethod]
        public void HashHalvesRoundTripThroughFieldHex()
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes("envelope"));
            var (hi, lo) = AztecLedgerKeyValueBackend.Halves(hash);
            // The SDK returns Fields as 64 hex digits; the halves must survive that padding.
            var joined = AztecLedgerKeyValueBackend.Join("0x" + hi[2..].PadLeft(64, '0'), "0x" + lo[2..].PadLeft(64, '0'));
            CollectionAssert.AreEqual(hash, joined);
        }

        [TestMethod]
        public void ZeroHalvesMeanNoRecord()
        {
            Assert.IsNull(AztecLedgerKeyValueBackend.Join("0x" + new string('0', 64), "0x0"));
        }

        [TestMethod]
        public void HalfWiderThan128BitsIsRejected()
        {
            Assert.ThrowsException<InvalidDataException>(() => AztecLedgerKeyValueBackend.Join("0x01" + new string('0', 62), "0x1"));
        }

        [TestMethod]
        public void RawCidMatchesKuboForAKnownBlock()
        {
            // `echo -n hello | ipfs add --cid-version=1 --raw-leaves` => this CID.
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hello"));
            Assert.AreEqual("bafkreibm6jg3ux5qumhcn2b3flc3tyu6dmlb4xa7u5bf44yegnrjhc4yeq", AztecLedgerKeyValueBackend.RawCid(hash));
        }
    }
}
