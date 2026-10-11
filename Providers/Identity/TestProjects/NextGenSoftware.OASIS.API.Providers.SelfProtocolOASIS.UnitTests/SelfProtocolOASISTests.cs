using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS.UnitTests
{
    /// <summary>Expected values were produced by the official @selfxyz/common 0.0.7 / @selfxyz/core 1.0.8 JavaScript.</summary>
    [TestClass]
    public class SelfProtocolOASISTests
    {
        [TestMethod]
        public void Scope_hash_matches_selfxyz_common()
        {
            Assert.AreEqual(BigInteger.Parse("13138896994318596904755311491546841890203015231385302139049524851758961713659"),
                SelfProtocolOASIS.HashEndpointWithScope("https://oasisomniverse.one/api/self/verify", "oasis-avatar"));
            Assert.AreEqual(BigInteger.Parse("4736000729600764200727744543968597404775404790138836703766569201647980033137"),
                SelfProtocolOASIS.HashEndpointWithScope("https://a-really-long-subdomain-name-for-testing.oasisomniverse.one/x", "s"));
            Assert.AreEqual(BigInteger.Parse("13119688108896310497414827204542281519712983648788472565985527177180129761025"),
                SelfProtocolOASIS.HashEndpointWithScope("localhost:5000", "x"));
        }

        [TestMethod]
        public void User_context_hash_matches_selfxyz_core()
        {
            var ctx = Convert.FromHexString("000000000000000000000000000000000000000000000000000000000000a4ec0000000000000000000000002967c5e6b3c4f179699bcc6e45bbe13b2203818e68656c6c6f");
            Assert.AreEqual(BigInteger.Parse("59259602982568130077922855798250529266791267489"), SelfProtocolOASIS.CalculateUserIdentifierHash(ctx));
        }

        [TestMethod]
        public void User_identifier_casts_match_selfxyz_common()
        {
            var v = BigInteger.Parse("2967c5e6b3c4f179699bcc6e45bbe13b2203818e", System.Globalization.NumberStyles.AllowHexSpecifier);
            Assert.AreEqual("0x2967c5e6b3c4f179699bcc6e45bbe13b2203818e", SelfProtocolOASIS.CastToUserIdentifier(v, SelfUserIdentifierType.Hex));
            var uuid = BigInteger.Parse("0123456789abcdef0123456789abcdef0", System.Globalization.NumberStyles.AllowHexSpecifier);
            Assert.AreEqual("12345678-9abc-def0-1234-56789abcdef0", SelfProtocolOASIS.CastToUserIdentifier(uuid, SelfUserIdentifierType.Uuid));
            Assert.AreEqual("00000000-0000-0000-0000-0000000000ff", SelfProtocolOASIS.CastToUserIdentifier(255, SelfUserIdentifierType.Uuid));
        }

        [TestMethod]
        public void Forbidden_countries_unpack_little_endian_31_byte_limbs()
        {
            var bytes = Encoding.ASCII.GetBytes("IRNPRKSYR");
            var limb = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
            CollectionAssert.AreEqual(new[] { "IRN", "PRK", "SYR" }, SelfProtocolOASIS.UnpackForbiddenCountries(new[] { limb, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero }).ToArray());
        }

        [TestMethod]
        public async Task Unknown_attestation_id_is_rejected_before_any_rpc()
        {
            var provider = new SelfProtocolOASIS("oasis", "https://oasisomniverse.one");
            var result = await provider.VerifyAsync(new SelfVerificationRequest { AttestationId = 9, PublicSignals = new string[21] }, new SelfVerificationConfig());
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "InvalidId");
        }

        /// <summary>Resolves the registry and Groth16 verifier through the real Celo Identity Verification Hub; a forged proof must not pass.</summary>
        [TestMethod, TestCategory("Live")]
        public async Task Live_hub_rejects_a_forged_proof()
        {
            var provider = new SelfProtocolOASIS("oasis", "https://oasisomniverse.one");
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var request = new SelfVerificationRequest
            {
                AttestationId = 1,
                A = new[] { "1", "2" },
                B = new[] { new[] { "1", "2" }, new[] { "3", "4" } },
                C = new[] { "1", "2" },
                PublicSignals = Enumerable.Repeat("0", 21).ToArray(),
                UserContextData = new string('0', 128)
            };
            var result = await provider.VerifyAsync(request, new SelfVerificationConfig());
            Assert.IsTrue(result.IsError, "a forged proof must fail");
            StringAssert.Contains(result.Message, "InvalidRoot");
        }
    }
}
