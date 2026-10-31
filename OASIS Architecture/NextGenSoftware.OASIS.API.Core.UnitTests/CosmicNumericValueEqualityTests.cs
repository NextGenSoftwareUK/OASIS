using FluentAssertions;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicNumericValueEqualityTests
{
    [Fact]
    public void ExactNumberOwnerSupportsExponentsBeyondInt64WithoutPowersOrDouble()
    {
        var number = new JRaw("1e9223372036854775808");
        StoredRecordValueEquality.CanonicalNumberText(number).Should().Be("1e9223372036854775808");
        StoredRecordValueEquality.Equivalent(number, new JRaw("10e9223372036854775807")).Should().BeTrue();
        StoredRecordValueEquality.Equivalent(number, new JRaw("1e9223372036854775807")).Should().BeFalse();
    }

    [Fact]
    public void ExactNumberOwnerKeepsZeroTextNullAndInvalidInputsDistinct()
    {
        StoredRecordValueEquality.Equivalent(new JRaw("-0e99999999999999999999999999"), new JValue(0L)).Should().BeTrue();
        StoredRecordValueEquality.Equivalent(new JRaw("1"), new JValue("1")).Should().BeFalse();
        StoredRecordValueEquality.IsNumber(JValue.CreateNull()).Should().BeFalse();
        Action text = () => StoredRecordValueEquality.CanonicalNumberText(new JValue("1"));
        text.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EquivalentRawAndDecimalMetadataDoesNotCreatePendingEdits()
    {
        const decimal amount = 1.000000000000000000000000001m;
        var row = new Holon { MetaData = new() { ["amount"] = amount } }; row.AcceptChanges();
        using var document = System.Text.Json.JsonDocument.Parse("1.0000000000000000000000000010");
        row.MetaData["amount"] = document.RootElement.Clone();
        row.HasHolonChanged().Should().BeFalse();
        HolonChangeTracking.PreviewValueChanges(row).Result.Should().BeEmpty();
    }

    [Fact]
    public void EquivalentArrayNumbersDoNotCreateValueDeltas()
    {
        var before = (JObject)StoredRecordJson.CaptureRuntimeToken(new { values = new object[] { 1m, 1.000000000000000000000000001m } });
        using var document = System.Text.Json.JsonDocument.Parse("{\"values\":[1.00,1.0000000000000000000000000010]}");
        var after = (JObject)StoredRecordJson.CaptureRuntimeToken(document.RootElement);
        HolonValueDelta.Compare(before, after).Should().BeEmpty();
    }

    [Fact]
    public void DistinctPreciseNumbersStillProduceExactDetachedDeltas()
    {
        var before = (JObject)StoredRecordJson.CaptureRuntimeToken(new { amount = 1.000000000000000000000000001m });
        using var document = System.Text.Json.JsonDocument.Parse("{\"amount\":1.000000000000000000000000002}");
        var after = (JObject)StoredRecordJson.CaptureRuntimeToken(document.RootElement);
        var delta = HolonValueDelta.Compare(before, after).Should().ContainSingle().Subject;
        delta.Path.Should().Be("/amount"); delta.PreviousJson.Should().Be("1.000000000000000000000000001");
        delta.CurrentJson.Should().Be("1.000000000000000000000000002");
    }

    [Fact]
    public void NumericTextRemainsDifferentFromANumber()
    {
        var before = new JObject { ["amount"] = 1m }; var after = new JObject { ["amount"] = "1.0" };
        HolonValueDelta.Compare(before, after).Should().ContainSingle();
    }
}
