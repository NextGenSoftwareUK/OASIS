using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class AkashPublicProviderCodecTests
{
    private const string Owner = "akash1u5cdg7k3gl43mukca4aeultuz8x2j68mgwn28e";

    [TestMethod]
    public void OfficialExampleKeyAndAllowlistPreserveExplicitUnknownDisplayName()
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new {
            owner = Owner, hostUri = "https://example.invalid:8443/", isOnline = true,
            name = (string?)null, email = "inert-private-marker", stats = new { arbitrary = "never copied" } }));
        var row = AkashPublicProviderCodec.Decode(document.RootElement, Owner);
        Assert.AreEqual(Owner, row.Owner); Assert.IsNull(row.DisplayName); Assert.IsTrue(row.IsOnline);
        Assert.AreEqual("https://example.invalid:8443/", row.HostUri);
        Assert.IsFalse(JsonSerializer.Serialize(row).Contains("inert-private-marker"));
    }

    [DataTestMethod]
    [DataRow("checksum")]
    [DataRow("uppercase")]
    [DataRow("prefix")]
    [DataRow("width")]
    public void InvalidOwnerIdentitiesAreRejected(string fault)
    {
        var key = fault switch { "checksum" => Owner[..^1] + "q", "uppercase" => Owner.ToUpperInvariant(),
            "prefix" => "cosmos" + Owner[5..], _ => Owner + "q" };
        Assert.ThrowsException<ArgumentException>(() => AkashPublicProviderCodec.ValidateOwner(key));
    }

    [DataTestMethod]
    [DataRow("owner")]
    [DataRow("duplicate")]
    [DataRow("error")]
    [DataRow("credentials")]
    [DataRow("scheme")]
    [DataRow("control")]
    [DataRow("label-width")]
    [DataRow("online-type")]
    public void AmbiguousOrMalformedProviderResponseFails(string fault)
    {
        var fields = new Dictionary<string, object?> { ["owner"] = Owner,
            ["hostUri"] = "https://example.invalid/", ["isOnline"] = true, ["name"] = "Provider" };
        if (fault == "owner") fields["owner"] = "another-provider";
        if (fault == "error") fields["error"] = false;
        if (fault == "credentials") fields["hostUri"] = "https://user:inert@example.invalid/";
        if (fault == "scheme") fields["hostUri"] = "javascript:inert";
        if (fault == "control") fields["name"] = "bad\nlabel";
        if (fault == "label-width") fields["name"] = new string('a', 257);
        if (fault == "online-type") fields["isOnline"] = "true";
        var json = JsonSerializer.Serialize(fields);
        if (fault == "duplicate") json = json.Insert(1, "\"owner\":\"duplicate\",");
        using var document = JsonDocument.Parse(json);
        try { AkashPublicProviderCodec.Decode(document.RootElement, Owner); Assert.Fail("Malformed provider accepted."); }
        catch (Exception error) when (error is InvalidDataException or InvalidOperationException) { }
    }
}
