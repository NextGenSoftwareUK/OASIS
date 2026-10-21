using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicDecimalTrackingTests
{
    public sealed class OwnedBytesHolon : Holon { public byte[] Payload { get; set; } = Array.Empty<byte>(); }
    private const decimal Before = 1.000000000000000000000000001m;
    private const decimal After = 1.000000000000000000000000002m;

    [Fact]
    public void JsonElementStringsAndContainersProjectAsValuesWithoutRawQuoting()
    {
        using var document = System.Text.Json.JsonDocument.Parse("{\"text\":\"ciphertext\",\"flag\":true,\"empty\":null,\"nested\":{\"number\":1.000000000000000000000000001},\"items\":[\"a\",false]}");
        var metadata = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object)p.Value.Clone());
        var copy = StoredRecordJson.Project<Holon>(new Holon { MetaData = metadata });
        copy.MetaData["text"].Should().BeOfType<string>().Which.Should().Be("ciphertext");
        copy.MetaData["flag"].Should().BeOfType<bool>().Which.Should().BeTrue();
        copy.MetaData["empty"].Should().BeNull();
        var json = StoredRecordJson.Serialize(copy.MetaData);
        json.Should().Contain("1.000000000000000000000000001").And.Contain("[\"a\",false]");
    }

    [Fact]
    public void ValidatedInputTokenPreservesPrecisionRootContractAndRejectionRules()
    {
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolon row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["amount"] = Before } };
        var token = StoredRecordJson.CaptureInputToken<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(row);
        token["$type"].Should().NotBeNull();
        token["MetaData"]!["amount"]!.ToObject<decimal>().Should().Be(Before);
        StoredRecordJson.SerializeInput(row).Should().Contain(Before.ToString(System.Globalization.CultureInfo.InvariantCulture));
        row.MetaData["callback"] = (Action)(() => { });
        Action captureDelegate = () => StoredRecordJson.CaptureInputToken(row);
        captureDelegate.Should().Throw<Newtonsoft.Json.JsonSerializationException>();
        row.MetaData.Remove("callback"); row.MetaData["cycle"] = row.MetaData;
        Action captureCycle = () => StoredRecordJson.CaptureInputToken(row);
        captureCycle.Should().Throw<Newtonsoft.Json.JsonSerializationException>();
    }

    [Fact]
    public void CanonicalTokenCaptureAndProjectionDetachByteArrays()
    {
        var row = new OwnedBytesHolon { Id = Guid.NewGuid(), Payload = new byte[] { 1, 2 } };
        var captured = StoredRecordJson.CaptureRuntimeToken(row);
        row.Payload[0] = 9;
        captured["Payload"]!.ToObject<byte[]>()!.Should().Equal(1, 2);
        var projected = StoredRecordJson.Project<OwnedBytesHolon>(row);
        projected.Payload.Should().NotBeSameAs(row.Payload); projected.Payload[0] = 8;
        row.Payload.Should().Equal(9, 2);
    }

    [Fact]
    public void RestoredByteArraysNeverAliasThePrivateAcceptedBaseline()
    {
        var row = new OwnedBytesHolon { Id = Guid.NewGuid(), Payload = new byte[] { 1, 2 } };
        row.AcceptChanges(); row.Payload[0] = 9;
        row.HasHolonChanged().Should().BeTrue();
        row.RejectChanges().IsError.Should().BeFalse(); row.Payload.Should().Equal(1, 2);
        row.Payload[0] = 8;
        row.HasHolonChanged().Should().BeTrue(); row.RejectChanges().IsError.Should().BeFalse();
        row.Payload.Should().Equal(1, 2);
    }

    [Fact]
    public void HighPrecisionCustomFieldEditsRemainDirtyAndPreviewExactValues()
    {
        var row = new CosmicTrackingTests.Sample { Id = Guid.NewGuid(), Price = Before };
        row.AcceptChanges(); row.Price = After;
        row.HasHolonChanged().Should().BeTrue();
        var preview = HolonChangeTracking.PreviewValueChanges(row);
        preview.IsError.Should().BeFalse(preview.Message);
        var change = preview.Result.Should().ContainSingle(d => d.Path == "/Price").Subject;
        change.PreviousJson.Should().Be(Before.ToString(System.Globalization.CultureInfo.InvariantCulture));
        change.CurrentJson.Should().Be(After.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void HighPrecisionNestedMetadataEditsRemainDirtyAndPreviewExactValues()
    {
        var nested = new Dictionary<string, object> { ["amount"] = Before };
        var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["nested"] = nested } };
        row.AcceptChanges(); nested["amount"] = After;
        row.HasHolonChanged().Should().BeTrue();
        var preview = HolonChangeTracking.PreviewValueChanges(row); preview.IsError.Should().BeFalse(preview.Message);
        var change = preview.Result.Should().ContainSingle(d => d.Path == "/MetaData/nested/amount").Subject;
        change.PreviousJson.Should().Be(Before.ToString(System.Globalization.CultureInfo.InvariantCulture));
        change.CurrentJson.Should().Be(After.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RejectChangesRestoresExactDecimalMetadataWithoutAliasingOriginal()
    {
        var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["amount"] = Before, ["wide"] = double.MaxValue } };
        row.AcceptChanges(); row.MetaData["amount"] = After; row.Original.MetaData["amount"] = 9m;
        var rejected = row.RejectChanges(); rejected.IsError.Should().BeFalse(rejected.Message);
        row.MetaData["amount"].Should().BeOfType<decimal>().Which.Should().Be(Before);
        row.MetaData["wide"].Should().BeOfType<double>().Which.Should().Be(double.MaxValue);
        row.HasHolonChanged().Should().BeFalse();
    }

    [Fact]
    public void CanonicalProjectionPreservesDecimalMetadataAndFullDoubleRange()
    {
        var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["amount"] = Before, ["wide"] = double.MaxValue } };
        var copy = StoredRecordJson.Project<Holon>(row);
        copy.MetaData["amount"].Should().BeOfType<decimal>().Which.Should().Be(Before);
        copy.MetaData["wide"].Should().BeOfType<double>().Which.Should().Be(double.MaxValue);
        row.MetaData["amount"] = After;
        copy.MetaData["amount"].Should().Be(Before);
    }

    [Fact]
    public void CanonicalProjectionPreservesRawJsonElementNumberPrecision()
    {
        using var document = System.Text.Json.JsonDocument.Parse("1.000000000000000000000000001");
        var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["amount"] = document.RootElement.Clone() } };
        var copy = StoredRecordJson.Project<Holon>(row);
        StoredRecordJson.Serialize(copy.MetaData).Should().Contain("1.000000000000000000000000001");
    }
}
