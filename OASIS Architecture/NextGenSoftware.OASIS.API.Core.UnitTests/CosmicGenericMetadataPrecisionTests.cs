using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicGenericMetadataPrecisionTests
{
    [Theory]
    [InlineData(0, 0)] [InlineData(0, 1)] [InlineData(0, 2)] [InlineData(0, 3)]
    [InlineData(1, 0)] [InlineData(1, 1)] [InlineData(1, 2)] [InlineData(1, 3)]
    [InlineData(2, 0)] [InlineData(2, 1)] [InlineData(2, 2)] [InlineData(2, 3)]
    public void GenericRecordReadersPreserveRawMetadataWithoutNarrowing(int kind, int route)
    {
        const decimal amount = 1.000000000000000000000000001m;
        const string precise = "1.00000000000000000000000000000000000000001";
        using var number = System.Text.Json.JsonDocument.Parse(precise);
        IHolonBase source = kind switch { 0 => new Holon(), 1 => new Avatar(), _ => new AvatarDetail() };
        source.Id = Guid.NewGuid(); source.Name = "canonical generic metadata";
        source.MetaData = new() { ["amount"] = amount, ["more"] = number.RootElement.Clone(), ["wide"] = double.MaxValue,
            ["text"] = "001.00", ["null"] = null! };
        var json = StoredRecordJson.Serialize<IHolonBase>(source);
        IHolonBase read;
        if (route == 2) read = (IHolonBase)StoredRecordJson.Deserialize(json, source.GetType());
        else if (route == 3) read = StoredRecordJson.Deserialize<IHolonBase>(json);
        else if (kind == 0) read = route == 0 ? StoredRecordJson.Deserialize<Holon>(json) : StoredRecordJson.DeserializeStored<Holon>(json);
        else if (kind == 1) read = route == 0 ? StoredRecordJson.Deserialize<Avatar>(json) : StoredRecordJson.DeserializeStored<Avatar>(json);
        else read = route == 0 ? StoredRecordJson.Deserialize<AvatarDetail>(json) : StoredRecordJson.DeserializeStored<AvatarDetail>(json);
        read.GetType().Should().Be(source.GetType()); read.Id.Should().Be(source.Id); read.Name.Should().Be(source.Name);
        var metadata = StoredRecordJson.Serialize(read.MetaData);
        metadata.Should().Contain(amount.ToString(System.Globalization.CultureInfo.InvariantCulture)).And.Contain(precise)
            .And.Contain(double.MaxValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).And.Contain("001.00");
        read.MetaData.Should().ContainKey("null"); read.MetaData["null"].Should().BeNull();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void GenericRecordReadersRejectAmbiguousMetadataAndUnsafeRootTypes(int route)
    {
        foreach (var json in new[] { "{\"MetaData\":null}", "{\"MetaData\":[]}", "{\"MetaData\":{},\"metadata\":{}}",
            "{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"MetaData\":{}}" })
        {
            Action read = () => {
                if (route == 0) StoredRecordJson.Deserialize<Avatar>(json);
                else if (route == 1) StoredRecordJson.DeserializeStored<AvatarDetail>(json);
                else StoredRecordJson.Deserialize(json, typeof(Holon));
            };
            if (route == 1) read.Should().Throw<System.IO.InvalidDataException>();
            else read.Should().Throw<Newtonsoft.Json.JsonException>();
        }
    }

    [Fact]
    public void TypedMetadataCollectionsRetainTheirRestrictedCanonicalContract()
    {
        var source = new Holon { MetaData = new() { ["history"] = new List<Dictionary<string, object>> {
            new() { ["amount"] = 25L, ["label"] = "entry" } } } };
        var read = StoredRecordJson.Deserialize<Holon>(StoredRecordJson.Serialize(source));
        var history = read.MetaData["history"].Should().BeOfType<List<Dictionary<string, object>>>().Subject;
        history.Single()["amount"].Should().Be(25L);
        history.Single()["label"].Should().Be("entry");
    }

    [Theory]
    [InlineData("{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"OriginalPath\":\"never-open\"}")]
    [InlineData("[{\"nested\":{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\"}}]")]
    public void RawMetadataCannotBypassRestrictedTypeBinding(string value)
    {
        Action read = () => StoredRecordJson.Deserialize<Holon>("{\"MetaData\":{\"unsafe\":" + value + "}}");
        read.Should().Throw<Newtonsoft.Json.JsonSerializationException>();
    }

    public sealed class PlainContainer { public string MetaData { get; set; } = ""; }

    [Fact]
    public void NonRecordAndNullReadContractsRemainUnchanged()
    {
        StoredRecordJson.Deserialize<decimal>("1.000000000000000000000000001").Should().Be(1.000000000000000000000000001m);
        StoredRecordJson.Deserialize<bool>("true").Should().BeTrue();
        StoredRecordJson.Deserialize<PlainContainer>("{\"MetaData\":\"literal\"}").MetaData.Should().Be("literal");
        StoredRecordJson.Deserialize<Holon>("null").Should().BeNull();
    }
}
