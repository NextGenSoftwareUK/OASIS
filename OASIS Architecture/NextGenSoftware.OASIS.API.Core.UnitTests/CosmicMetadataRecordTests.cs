using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicMetadataRecordTests
{
    public sealed class StorageDto
    {
        public int Count { get; set; }
        public long Sequence { get; set; }
        public decimal Price { get; set; }
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> MetaData { get; set; } = new() { ["default"] = "owned" };
    }
    public sealed class NullableDto { public Dictionary<string, object>? MetaData { get; set; } }
    public sealed class InvalidDto { public string MetaData { get; set; } = ""; }

    [Fact]
    public void ExplicitDtoDecoderPreservesTypedScalarsAndExactUntypedMetadata()
    {
        var id=Guid.NewGuid();
        var json="{\"Count\":2147483647,\"Sequence\":9223372036854775807,\"Price\":1.000000000000000000000000001,\"Id\":\""+id+"\",\"Timestamp\":\"2026-10-20T12:00:00Z\",\"MetaData\":{\"amount\":1.00000000000000000000000000000000000000001,\"wide\":1.7976931348623157E+308,\"text\":\"001.00\",\"date\":\"2026-10-20T12:00:00Z\",\"nothing\":null}}";
        var record=StoredRecordJson.DeserializeMetadataRecord<StorageDto>(json);
        Assert.Equal(int.MaxValue,record.Count); Assert.Equal(long.MaxValue,record.Sequence);
        Assert.Equal(1.000000000000000000000000001m,record.Price); Assert.Equal(id,record.Id);
        Assert.Equal(DateTimeKind.Utc,record.Timestamp.Kind);
        var metadata=StoredRecordJson.Serialize(record.MetaData);
        Assert.Contains("1.00000000000000000000000000000000000000001",metadata);
        Assert.Contains("1.7976931348623157E+308",metadata); Assert.Equal("001.00",record.MetaData["text"]);
        Assert.Equal("2026-10-20T12:00:00Z",record.MetaData["date"]); Assert.Null(record.MetaData["nothing"]);
    }

    [Theory]
    [InlineData("{\"MetaData\":[]}")]
    [InlineData("{\"MetaData\":{},\"metadata\":{}}")]
    [InlineData("{\"MetaData\":{\"value\":{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\"}}}")]
    [InlineData("{\"MetaData\":{\"value\":[{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\"}]}}")]
    [InlineData("{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"MetaData\":{}}")]
    public void ExplicitDtoDecoderRetainsSchemaAndRestrictedBinderAdmission(string json)
        => Assert.ThrowsAny<Newtonsoft.Json.JsonException>(()=>StoredRecordJson.DeserializeMetadataRecord<StorageDto>(json));

    [Fact]
    public void ExplicitDtoDecoderRequiresDeclaredMetadataAndRetainsNullOrMissingContracts()
    {
        Assert.Throws<ArgumentException>(()=>StoredRecordJson.DeserializeMetadataRecord<InvalidDto>("{}"));
        Assert.Equal("owned",StoredRecordJson.DeserializeMetadataRecord<StorageDto>("{}").MetaData["default"]);
        Assert.Equal("owned",StoredRecordJson.DeserializeMetadataRecord<StorageDto>("{\"MetaData\":null}").MetaData["default"]);
        Assert.Null(StoredRecordJson.DeserializeMetadataRecord<StorageDto>("null"));
        Assert.Null(StoredRecordJson.DeserializeMetadataRecord<NullableDto>("{\"MetaData\":null}").MetaData);
        Assert.Equal("literal",StoredRecordJson.Deserialize<InvalidDto>("{\"MetaData\":\"literal\"}").MetaData);
    }
}
