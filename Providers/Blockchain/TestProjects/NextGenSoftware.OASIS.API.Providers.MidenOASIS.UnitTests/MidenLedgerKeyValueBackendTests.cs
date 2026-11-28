using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS.UnitTests
{
    [TestClass]
    public class MidenLedgerKeyValueBackendTests
    {
        [TestMethod]
        public void RecordIdIsThreeBigEndianLimbsOfTheKeySha256()
        {
            var digest = SHA256.HashData(Encoding.UTF8.GetBytes("oasis/holon/abc"));
            var id = MidenLedgerKeyValueBackend.RecordId("oasis/holon/abc");
            Assert.AreEqual(3, id.Length);
            Assert.AreEqual(((uint)(digest[0] << 24 | digest[1] << 16 | digest[2] << 8 | digest[3])).ToString(), id[0]);
            CollectionAssert.AreEqual(new[] { id[0], id[1], id[2], "1" }, MidenLedgerKeyValueBackend.MapKey(id, 1));
        }

        [TestMethod]
        public void HashHalvesRoundTripThroughLimbs()
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes("envelope"));
            var joined = MidenLedgerKeyValueBackend.Join(MidenLedgerKeyValueBackend.ValueLimbs(hash, 0), MidenLedgerKeyValueBackend.ValueLimbs(hash, 1));
            CollectionAssert.AreEqual(hash, joined);
        }

        [TestMethod]
        public void ZeroWordsMeanNoRecord()
        {
            var zero = new[] { "0", "0", "0", "0" };
            Assert.IsNull(MidenLedgerKeyValueBackend.Join(zero, zero));
        }

        [TestMethod]
        public void LimbWiderThan32BitsIsRejected()
        {
            Assert.ThrowsException<InvalidDataException>(() =>
                MidenLedgerKeyValueBackend.Join(new[] { "4294967296", "0", "0", "0" }, new[] { "0", "0", "0", "1" }));
        }

        [TestMethod]
        public void RawCidMatchesKuboForAKnownBlock()
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes("hello"));
            Assert.AreEqual("bafkreibm6jg3ux5qumhcn2b3flc3tyu6dmlb4xa7u5bf44yegnrjhc4yeq", MidenLedgerKeyValueBackend.RawCid(hash));
        }
    }
}
