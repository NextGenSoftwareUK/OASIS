using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicReceiptPrecisionTests
{
    [Fact]
    public void StoredHolonReaderPreservesArbitraryMetadataNumberLexemesAndTypedFields()
    {
        var id = Guid.NewGuid();
        var json = "{\"Id\":\"" + id + "\",\"Version\":7,\"MetaData\":{\"decimal\":1.000000000000000000000000001,\"longer\":1.00000000000000000000000000000000000000001,\"wide\":1.7976931348623157E+308,\"nested\":[null,\"001.00\"]}}";
        var row = StoredRecordJson.DeserializeStoredHolon(json);
        row.Id.Should().Be(id); row.Version.Should().Be(7);
        var values = StoredRecordJson.Serialize(row.MetaData);
        values.Should().Contain("1.000000000000000000000000001").And.Contain("1.00000000000000000000000000000000000000001")
            .And.Contain("1.7976931348623157E+308").And.Contain("[null,\"001.00\"]");
        ((System.Text.Json.JsonElement)row.MetaData["decimal"]).GetDecimal().Should().Be(1.000000000000000000000000001m);
    }

    [Fact]
    public void StoredHolonReaderRejectsAmbiguousAndNonObjectMetadata()
    {
        foreach (var json in new[] { "[]", "{\"MetaData\":null,\"metadata\":null}", "{\"MetaData\":2}", "{\"MetaData\":[]}",
            "{\"MetaData\":{},\"metadata\":{}}", "{\"MetaData\":{},\"MetaData\":{}}", "{" })
        {
            Action read = () => StoredRecordJson.DeserializeStoredHolon(json);
            read.Should().Throw<System.IO.InvalidDataException>();
        }
    }

    [Fact]
    public void StoredHolonReaderKeepsMetadataTypeNamesAsDataAndStillRejectsUnsafeRootTypes()
    {
        var row = StoredRecordJson.DeserializeStoredHolon("{\"MetaData\":{\"value\":{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"name\":\"literal\"}}}");
        var value = (System.Text.Json.JsonElement)row.MetaData["value"];
        value.GetProperty("$type").GetString().Should().Be("System.IO.FileInfo, System.Private.CoreLib");
        Action unsafeRoot = () => StoredRecordJson.DeserializeStoredHolon("{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"MetaData\":{}}");
        unsafeRoot.Should().Throw<System.IO.InvalidDataException>();
    }

    [Fact]
    public void StoredHolonReaderRetainsExplicitNullAndNormalStringsAfterDocumentDisposal()
    {
        var row = StoredRecordJson.DeserializeStoredHolon("{\"Name\":\"receipt\",\"MetaData\":{\"missing-value\":null,\"cipher\":\"opaque-text\",\"bool\":false}}");
        row.Name.Should().Be("receipt"); row.MetaData.Should().ContainKey("missing-value"); row.MetaData["missing-value"].Should().BeNull();
        row.MetaData["cipher"].ToString().Should().Be("opaque-text");
        ((System.Text.Json.JsonElement)row.MetaData["bool"]).GetBoolean().Should().BeFalse();
        StoredRecordJson.DeserializeStoredHolon("{}").MetaData.Should().NotBeNull();
    }
}
