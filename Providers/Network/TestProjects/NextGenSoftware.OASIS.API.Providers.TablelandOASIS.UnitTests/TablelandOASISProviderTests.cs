using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TablelandOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TablelandOASIS.UnitTests
{
    /// <summary>
    /// Backend-free checks: configuration validation, SQL literal construction and table-name parsing.
    /// Storage behaviour is covered by the native conformance suite against a real local Tableland network.
    /// </summary>
    [TestClass]
    public class TablelandOASISProviderTests
    {
        private const string DevKey = "0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d";
        private const string Registry = "0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512";

        private static TablelandConnection Connection(string rpc = "http://127.0.0.1:8545")
            => new(new Uri(rpc), new Uri("http://127.0.0.1:8080/"), Registry, 31337, DevKey);

        private static TablelandOASIS Provider(string rpc = "http://127.0.0.1:8545")
            => new(Connection(rpc), "oasis_kv_31337_7", new byte[32]);

        /// <summary>Decodes an expression produced by Literal back to its value, checking every literal's size.</summary>
        private static string Decode(string expression)
        {
            var parts = Regex.Matches(expression, "'((?:[^']|'')*)'");
            Assert.AreEqual(expression, string.Join(" || ", parts.Select(m => m.Value)), "Expression must be literals joined by ||.");
            foreach (Match part in parts)
                Assert.IsTrue(Encoding.UTF8.GetByteCount(part.Groups[1].Value) <= TablelandKeyValueBackend.MaxTextLiteralBytes);
            return string.Concat(parts.Select(m => m.Groups[1].Value.Replace("''", "'")));
        }

        [TestMethod]
        public void Provider_IdentifiesItselfAndStartsDeactivated()
        {
            var provider = Provider();
            Assert.AreEqual(ProviderType.TablelandOASIS, provider.ProviderType.Value);
            Assert.AreEqual("TablelandOASIS", provider.ProviderName);
            Assert.AreEqual("oasis_kv_31337_7", provider.TableName);
            Assert.AreEqual("0x70997970C51812dc3A010C7d01b50e0d17dc79C8", provider.WalletAddress);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void Provider_RequiresA32ByteEncryptionKey()
        {
            Assert.ThrowsException<ArgumentException>(() => new TablelandOASIS(Connection(), "oasis_kv_31337_7", new byte[16]));
            Assert.ThrowsException<ArgumentException>(() => new TablelandOASIS(Connection(), "oasis_kv_31337_7", null!));
        }

        [TestMethod]
        public void Connection_RejectsMissingSettings()
        {
            Assert.ThrowsException<ArgumentException>(() => new TablelandConnection(new Uri("http://a"), new Uri("http://b"), "", 31337, DevKey));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new TablelandConnection(new Uri("http://a"), new Uri("http://b"), Registry, 0, DevKey));
            Assert.ThrowsException<ArgumentException>(() => new TablelandConnection(new Uri("http://a"), new Uri("http://b"), Registry, 31337, " "));
        }

        [TestMethod]
        public void TableName_MustMatchTheConfiguredChain()
        {
            Assert.AreEqual((new System.Numerics.BigInteger(42), "oasis_kv"), TablelandKeyValueBackend.ParseTableName("oasis_kv_31337_42", 31337));
            foreach (var bad in new[] { "oasis_kv_1_42", "oasis_kv_31337_0", "oasis_kv_31337_x", "31337_42", "_a_31337_1", "t; DROP_31337_1", "x_31337_1 --" })
                Assert.ThrowsException<ArgumentException>(() => TablelandKeyValueBackend.ParseTableName(bad, 31337), bad);
            Assert.ThrowsException<ArgumentException>(() => new TablelandOASIS(Connection(), "oasis_kv_1_7", new byte[32]));
        }

        [TestMethod]
        public void Literal_EscapesQuotesAndRejectsNul()
        {
            Assert.AreEqual("''", TablelandKeyValueBackend.Literal(""));
            Assert.AreEqual("'it''s'", TablelandKeyValueBackend.Literal("it's"));
            Assert.AreEqual("x'); DELETE FROM t; --", Decode(TablelandKeyValueBackend.Literal("x'); DELETE FROM t; --")));
            Assert.ThrowsException<ArgumentException>(() => TablelandKeyValueBackend.Literal("a\0b"));
        }

        [TestMethod]
        public void Literal_SplitsLongValuesIntoParserSizedLiteralsWithoutBreakingPairs()
        {
            var value = string.Concat(Enumerable.Range(0, 900).Select(i => i % 7 == 0 ? "'" : i % 5 == 0 ? "\U0001F600" : "ab"));
            var expression = TablelandKeyValueBackend.Literal(value);
            Assert.IsTrue(expression.Contains(" || "), "A value longer than one literal must be concatenated.");
            Assert.AreEqual(value, Decode(expression));

            var ascii = new string('z', 3000);
            Assert.AreEqual(3, Regex.Matches(TablelandKeyValueBackend.Literal(ascii), "'z+'").Count);
            Assert.AreEqual(ascii, Decode(TablelandKeyValueBackend.Literal(ascii)));
        }

        [TestMethod]
        public async Task Activation_FailsWhenTheChainIsUnreachable()
        {
            var result = await Provider("http://127.0.0.1:1").ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.Result);
        }
    }
}
