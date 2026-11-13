using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS.UnitTests;

[TestClass]
public sealed class NostrPublicEventVerificationTests
{
    private static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", name), Encoding.UTF8);

    public static IEnumerable<object[]> Vectors()
    {
        using var file = JsonDocument.Parse(Fixture("bip340-public-vectors.json"));
        foreach (var row in file.RootElement.GetProperty("vectors").EnumerateArray())
            yield return new object[] { row.GetProperty("index").GetInt32(), row.GetProperty("publicKey").GetString()!,
                row.GetProperty("message").GetString()!, row.GetProperty("signature").GetString()!, row.GetProperty("expected").GetBoolean() };
    }

    [DataTestMethod]
    [DynamicData(nameof(Vectors), DynamicDataSourceType.Method)]
    public void OfficialPublicBip340Vectors(int index, string key, string message, string signature, bool expected)
        => Assert.AreEqual(expected, NostrPublicEventVerification.VerifySchnorr(Convert.FromHexString(key),
            Convert.FromHexString(message), Convert.FromHexString(signature)), "Official vector " + index);

    [TestMethod]
    public void OriginalSignedPublicEventRoundTripsExactFields()
    {
        string text = Fixture("captured-public-event.json");
        var original = JsonNode.Parse(text)!;
        var result = NostrPublicEventVerification.Parse(text);
        Assert.AreEqual(original["id"]!.GetValue<string>(), result.Id);
        Assert.AreEqual(original["pubkey"]!.GetValue<string>(), result.PublicKey);
        Assert.AreEqual(original["created_at"]!.GetValue<long>(), result.CreatedAt);
        Assert.AreEqual(original["content"]!.GetValue<string>(), result.Content);
        Assert.IsTrue(JsonNode.DeepEquals(original["tags"], JsonNode.Parse(result.TagsJson)));
    }

    [DataTestMethod]
    [DataRow("id")]
    [DataRow("pubkey")]
    [DataRow("sig")]
    [DataRow("content")]
    [DataRow("created_at")]
    [DataRow("tags")]
    public void MutationOfSignedFieldsIsRejected(string field)
    {
        var node = JsonNode.Parse(Fixture("captured-public-event.json"))!;
        node[field] = field switch
        {
            "created_at" => JsonValue.Create(0), "tags" => new JsonArray(new JsonArray(JsonValue.Create("modified"))),
            "content" => JsonValue.Create("modified public text"), _ => JsonValue.Create(new string('0', field == "sig" ? 128 : 64))
        };
        Assert.ThrowsException<InvalidDataException>(() => NostrPublicEventVerification.Parse(node.ToJsonString()));
    }

    [DataTestMethod]
    [DataRow("kind", "true")]
    [DataRow("kind", "0")]
    [DataRow("created_at", "-1")]
    [DataRow("created_at", "9007199254740992")]
    [DataRow("created_at", "1e3")]
    [DataRow("tags", "[[]]")]
    [DataRow("tags", "[[null]]")]
    [DataRow("content", "null")]
    public void InvalidNativeTypesAreRejected(string field, string raw)
    {
        var node = JsonNode.Parse(Fixture("captured-public-event.json"))!;
        node[field] = JsonNode.Parse(raw);
        Assert.ThrowsException<InvalidDataException>(() => NostrPublicEventVerification.Parse(node.ToJsonString()));
    }

    [TestMethod]
    public void CanonicalJsonRetainsUnicodeScalarsAndEscapesOnlyRequiredCharacters()
    {
        string input = "café/<>\u2028\u2029\U0001f680\n\r\t\b\f\"\\\u0001";
        Assert.AreEqual("\"café/<>\u2028\u2029\U0001f680\\n\\r\\t\\b\\f\\\"\\\\\\u0001\"", NostrPublicEventVerification.Quote(input));
        Assert.ThrowsException<EncoderFallbackException>(() => NostrPublicEventVerification.Quote("\ud800"));
    }

    [TestMethod]
    public void DuplicatePropertiesOversizedBodiesAndSignatureWidthsReject()
    {
        string text = Fixture("captured-public-event.json");
        Assert.ThrowsException<InvalidDataException>(() => NostrPublicEventVerification.Parse(text.Insert(text.IndexOf('{') + 1, "\"kind\":1,")));
        Assert.ThrowsException<InvalidDataException>(() => NostrPublicEventVerification.Parse(new string(' ', 65537)));
        Assert.IsFalse(NostrPublicEventVerification.VerifySchnorr(new byte[31], new byte[32], new byte[64]));
        Assert.IsFalse(NostrPublicEventVerification.VerifySchnorr(new byte[32], new byte[32], new byte[63]));
    }
}
