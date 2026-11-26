using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

public sealed class ReplicaRetireHeldCommandTests
{
    private static Dictionary<string, object> Valid() => new()
    {
        ["OperatorId"] = Guid.NewGuid(), ["Sequence"] = 14L, ["SourceOperationId"] = Guid.NewGuid(), ["AttemptCount"] = 1,
        ["SourcePayloadDigest"] = new string('A', 64), ["SourceStoreIdentity"] = "neo4j-store:ABC",
        ["TargetFile"] = "/data/OASISSQLLiteDB.db", ["RootId"] = Guid.NewGuid(),
        ["PayloadVersionId"] = Guid.NewGuid(), ["PayloadVersion"] = 1,
        ["SupersedingVersionId"] = Guid.NewGuid(), ["SupersedingVersion"] = 18
    };

    private static byte[] Json(object value) => JsonSerializer.SerializeToUtf8Bytes(value);

    [Fact]
    public void ParsesACompleteReviewedRetirement()
    {
        var request = ReplicaRetireHeldCommand.Parse(Json(Valid()));
        Assert.Equal(14L, request.Sequence);
        Assert.Equal(18, request.SupersedingVersion);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public void RefusesAHeadThatIsNotNewer(int kept)
    {
        var value = Valid(); value["SupersedingVersion"] = kept;
        Assert.Throws<InvalidDataException>(() => ReplicaRetireHeldCommand.Parse(Json(value)));
    }

    [Fact]
    public void RefusesUnknownFields()
    {
        var value = Valid(); value["Force"] = true;
        Assert.ThrowsAny<JsonException>(() => ReplicaRetireHeldCommand.Parse(Json(value)));
    }

    [Fact]
    public void RefusesDuplicateFields()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("{\"Sequence\":14,\"Sequence\":15}");
        Assert.Throws<InvalidDataException>(() => ReplicaRetireHeldCommand.Parse(bytes));
    }

    [Fact]
    public void RefusesAnIncompleteRequest()
    {
        var value = Valid(); value.Remove("SourcePayloadDigest");
        Assert.Throws<InvalidDataException>(() => ReplicaRetireHeldCommand.Parse(Json(value)));
    }
}
