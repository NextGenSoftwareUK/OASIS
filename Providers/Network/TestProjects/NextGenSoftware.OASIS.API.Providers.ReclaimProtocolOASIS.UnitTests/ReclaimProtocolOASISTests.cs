using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ReclaimProtocolOASIS.UnitTests
{
    /// <summary>Uses a real attestor-signed proof taken from the official reclaim-js-sdk test suite.</summary>
    [TestClass]
    public class ReclaimProtocolOASISTests
    {
        private const string ProviderHash = "0x4c20776ae89ab7eead49e4e393f4e07348a4d85e21869201aa6eea6e2bc07f5b";
        private static readonly string Proof = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "reclaim-proof.json"));

        private sealed class Attestors : HttpMessageHandler
        {
            private readonly string _json;
            public int Calls;
            public Attestors(params string[] addresses) =>
                _json = "{\"data\":[" + string.Join(",", Array.ConvertAll(addresses, a => $"{{\"address\":\"{a}\"}}")) + "]}";
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Calls++;
                Assert.AreEqual("/api/attestors", request.RequestUri.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
            }
        }

        private static ReclaimProtocolOASIS With(Attestors a) => new("https://api.reclaimprotocol.org", a);

        private static string Tamper(Func<string, string> edit) => edit(Proof);

        [TestMethod]
        public async Task Genuine_proof_verifies_against_registered_attestor()
        {
            var result = await With(new Attestors("0x1be31a94361a391bbafb2a4ccd704f57dc04d4bb", "0x244897572368eadf65bfbc5aec98d8e5443a9072")).VerifyProofAsync(Proof, ProviderHash);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual("0x51c192777d45010e9318c0e1eb2fefc0bc5a444f59e3d3e5a11e9a3d1b98e10c", result.Result.Identifier);
            Assert.AreEqual("0x244897572368eadf65bfbc5aec98d8e5443a9072", result.Result.Attestors[0]);
            Assert.AreEqual("mushaheedsyed", result.Result.Context.GetProperty("extractedParameters").GetProperty("username").GetString());
        }

        [TestMethod]
        public async Task Signer_outside_the_attestor_set_is_rejected()
        {
            var result = await With(new Attestors("0x1be31a94361a391bbafb2a4ccd704f57dc04d4bb")).VerifyProofAsync(Proof);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "registered Reclaim attestor");
        }

        [TestMethod]
        public async Task Altered_owner_breaks_the_signature()
        {
            var tampered = Tamper(p => p.Replace("0x2967c5e6b3c4f179699bcc6e45bbe13b2203818e", "0xb737a1f20b2b556e79f715f86098f205a2730bc5"));
            var result = await With(new Attestors("0x244897572368eadf65bfbc5aec98d8e5443a9072")).VerifyProofAsync(tampered);
            Assert.IsTrue(result.IsError);
        }

        [TestMethod]
        public async Task Altered_context_does_not_match_identifier()
        {
            var tampered = Tamper(p => p.Replace("mushaheedsyed\\\"", "someoneelse\\\""));
            Assert.AreNotEqual(Proof, tampered);
            var attestors = new Attestors("0x244897572368eadf65bfbc5aec98d8e5443a9072");
            var result = await With(attestors).VerifyProofAsync(tampered);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "identifier does not match");
            Assert.AreEqual(0, attestors.Calls);
        }

        [TestMethod]
        public async Task Wrong_provider_hash_is_rejected()
        {
            var result = await With(new Attestors("0x244897572368eadf65bfbc5aec98d8e5443a9072")).VerifyProofAsync(Proof, "0x" + new string('0', 64));
            Assert.IsTrue(result.IsError);
        }

        [TestMethod]
        public void Canonical_numbers_match_ecmascript()
        {
            foreach (var (input, expected) in new[] { ("1", "1"), ("1.5", "1.5"), ("100", "100"), ("1e21", "1e+21"), ("1e-7", "1e-7"), ("0.000001", "0.000001"), ("-2.25", "-2.25"), ("123456789012", "123456789012") })
                Assert.AreEqual(expected, JsonCanonicalizer.Canonicalize(JsonDocument.Parse(input).RootElement), input);
            Assert.AreEqual(@"{""a"":[1,""\n""],""b"":true}", JsonCanonicalizer.Canonicalize(JsonDocument.Parse(@"{ ""b"": true, ""a"": [1.0, ""\n""] }").RootElement));
        }

        /// <summary>Verifies the genuine proof against the live attestor registry.</summary>
        [TestMethod, TestCategory("Live")]
        public async Task Live_attestor_registry_accepts_genuine_proof()
        {
            var result = await new ReclaimProtocolOASIS().VerifyProofAsync(Proof, ProviderHash);
            Assert.IsFalse(result.IsError, result.Message);
        }
    }
}
