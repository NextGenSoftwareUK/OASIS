using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

[TestClass]
public sealed class TonPublicHeaderReferenceTests
{
    private static readonly string Root = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray());
    private static readonly string FileHash = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray());

    private static Dictionary<string, object> Block(uint sequence) => new() {
        ["@type"] = "ton.blockIdExt", ["workchain"] = -1,
        ["shard"] = TonPublicHeaderReference.MasterchainShard,
        ["seqno"] = sequence, ["root_hash"] = Root, ["file_hash"] = FileHash
    };

    private static Dictionary<string, object> Header() => new() {
        ["@type"] = "blocks.header", ["id"] = Block(10), ["global_id"] = -239,
        ["prev_blocks"] = new[] { Block(9) }, ["start_lt"] = "9007199254740993",
        ["end_lt"] = "18446744073709551615", ["version"] = 0,
        ["gen_utime"] = uint.MaxValue, ["validator_list_hash_short"] = int.MaxValue,
        ["catchain_seqno"] = 1, ["min_ref_mc_seqno"] = 9, ["prev_key_block_seqno"] = 8,
        ["after_merge"] = false, ["after_split"] = false, ["before_split"] = false,
        ["want_merge"] = true, ["want_split"] = false, ["is_key_block"] = false
    };

    [TestMethod]
    public void ExactLogicalTimesAndUInt32BoundarySurviveValidation()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new { ok = true, result = Header() }));
        var reference = new TonPublicHeaderReference(10, Root, FileHash);
        JsonElement header = reference.ValidateHeader(document.RootElement);

        Assert.AreEqual("9007199254740993", header.GetProperty("start_lt").GetString());
        Assert.AreEqual(ulong.MaxValue, TonPublicHeaderReference.LogicalTime(header.GetProperty("end_lt")));
        Assert.AreEqual(uint.MaxValue, TonPublicHeaderReference.UInt32Token(header.GetProperty("gen_utime")));
    }

    [DataTestMethod]
    [DataRow(-2147483648)] [DataRow(-564262473)] [DataRow(2147483647)]
    public void SignedValidatorHashFragmentRetainsItsExactWireValue(int value)
    {
        var header = Header();
        header["validator_list_hash_short"] = value;
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new { ok = true, result = header }));
        var validated = new TonPublicHeaderReference(10, Root, FileHash).ValidateHeader(document.RootElement);
        Assert.AreEqual(value, validated.GetProperty("validator_list_hash_short").GetInt32());
    }

    [DataTestMethod]
    [DataRow("2147483648")] [DataRow("-2147483649")] [DataRow("-0")]
    [DataRow("1.5")] [DataRow("1e0")] [DataRow("\"1\"")]
    public void InvalidSignedHashTokenIsRejected(string token)
    {
        using var document = JsonDocument.Parse(token);
        Assert.ThrowsException<InvalidDataException>(() => TonPublicHeaderReference.Int32Token(document.RootElement));
    }

    [DataTestMethod]
    [DataRow("network")] [DataRow("sequence")] [DataRow("workchain")]
    [DataRow("shard")] [DataRow("root")] [DataRow("file")] [DataRow("parent")]
    [DataRow("no-parent")] [DataRow("logical-number")] [DataRow("logical-overflow")]
    [DataRow("logical-leading-zero")] [DataRow("reversed")] [DataRow("negative")]
    [DataRow("fraction")] [DataRow("number-string")] [DataRow("overflow")]
    [DataRow("protocol")] [DataRow("duplicate-json")]
    public void RejectWrongIdentityAndMalformedNativeDomains(string fault)
    {
        var header = Header();
        var id = (Dictionary<string, object>)header["id"];
        bool ok = true;
        if (fault == "network") header["global_id"] = -3;
        if (fault == "sequence") id["seqno"] = 11;
        if (fault == "workchain") id["workchain"] = 0;
        if (fault == "shard") id["shard"] = "0";
        if (fault == "root") id["root_hash"] = FileHash;
        if (fault == "file") id["file_hash"] = Root;
        if (fault == "parent") header["prev_blocks"] = new[] { Block(8) };
        if (fault == "no-parent") header["prev_blocks"] = Array.Empty<object>();
        if (fault == "logical-number") header["start_lt"] = 1;
        if (fault == "logical-overflow") header["end_lt"] = "18446744073709551616";
        if (fault == "logical-leading-zero") header["start_lt"] = "01";
        if (fault == "reversed") header["end_lt"] = "0";
        if (fault == "negative") header["gen_utime"] = -1;
        if (fault == "fraction") header["gen_utime"] = 1.5;
        if (fault == "number-string") header["gen_utime"] = "1";
        if (fault == "overflow") header["gen_utime"] = 4294967296UL;
        if (fault == "protocol") ok = false;

        string json = JsonSerializer.Serialize(new { ok, result = header });
        if (fault == "duplicate-json") json = json.Replace("\"ok\":true", "\"ok\":true,\"ok\":true");
        using var document = JsonDocument.Parse(json);
        var reference = new TonPublicHeaderReference(10, Root, FileHash);
        Assert.ThrowsException<InvalidDataException>(() => reference.ValidateHeader(document.RootElement));
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("block-shard")] [DataRow("wrong-anchor")]
    public void ZeroStatePlaceholderIsDistinctFromARealBlock(string scenario)
    {
        var init = Block(0);
        init["shard"] = scenario == "block-shard" ? TonPublicHeaderReference.MasterchainShard : "0";
        init["root_hash"] = scenario == "wrong-anchor" ? Root : TonPublicHeaderReference.GenesisRootHash;
        init["file_hash"] = TonPublicHeaderReference.GenesisFileHash;
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new {
            ok = true, result = new Dictionary<string, object> {
                ["@type"] = "blocks.masterchainInfo", ["init"] = init
            }
        }));

        if (scenario == "normal") TonPublicHeaderReference.ValidateMainnetIdentity(document.RootElement);
        else Assert.ThrowsException<InvalidDataException>(() =>
            TonPublicHeaderReference.ValidateMainnetIdentity(document.RootElement));
    }

    [TestMethod]
    public void DescriptorRejectsZeroSequenceAndNoncanonicalHashes()
    {
        Assert.ThrowsException<ArgumentException>(() => new TonPublicHeaderReference(0, Root, FileHash));
        Assert.ThrowsException<InvalidDataException>(() => new TonPublicHeaderReference(1, "bad", FileHash));
        Assert.ThrowsException<InvalidDataException>(() => new TonPublicHeaderReference(1, new string('B', 43) + "=", FileHash));
    }

    [TestMethod]
    [TestCategory("CapturedTonHeaderContract")]
    public void QualifiedNativeResponsesPassTheSameDecoder()
    {
        string? path = Environment.GetEnvironmentVariable("COSMIC_TON_HEADER_FIXTURES");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained public wire fixtures required.");
        using var identity = JsonDocument.Parse(System.IO.File.ReadAllBytes(Path.Combine(path!, "masterchain-info.response.json")));
        TonPublicHeaderReference.ValidateMainnetIdentity(identity.RootElement);

        foreach (int sequence in new[] { 10000000, 10000001, 10000002 })
        {
            using var original = JsonDocument.Parse(System.IO.File.ReadAllBytes(Path.Combine(path!, $"header-{sequence}.response.json")));
            var id = original.RootElement.GetProperty("result").GetProperty("id");
            var reference = new TonPublicHeaderReference((uint)sequence,
                id.GetProperty("root_hash").GetString()!, id.GetProperty("file_hash").GetString()!);
            reference.ValidateHeader(original.RootElement);

            if (sequence != 10000001)
            {
                using var subset = JsonDocument.Parse(System.IO.File.ReadAllBytes(Path.Combine(path!, $"subset-{sequence}.response.json")));
                reference.ValidateHeader(subset.RootElement);
            }
        }
    }
}
