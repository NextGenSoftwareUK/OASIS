using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed class HeldHeadRearmCommandTests
{
    private static Dictionary<string, object> Valid() => new()
    {
        ["OperatorId"] = Guid.NewGuid(), ["Sequence"] = 890L, ["SourceOperationId"] = Guid.NewGuid(),
        ["SourcePayloadDigest"] = new string('a', 64), ["TargetId"] = "hyperdrive-target-v2:SQLLiteDBOASIS:abc",
        ["ErrorCode"] = "COSMIC_REPLICA_AUTHORIZATION_DENIED", ["SourceStoreIdentity"] = "mongo-store:ABC"
    };

    private static byte[] Json(object value) => JsonSerializer.SerializeToUtf8Bytes(value);

    [Fact]
    public void ParsesACompleteReviewedRequest()
    {
        var request = HeldHeadRearmCommand.Parse(Json(Valid()));
        Assert.Equal(890L, request.Sequence);
        Assert.Equal("COSMIC_REPLICA_AUTHORIZATION_DENIED", request.ErrorCode);
    }

    [Theory]
    [InlineData("COSMIC_REPLICA_LINEAGE_CONFLICT")]
    [InlineData("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH")]
    [InlineData("COSMIC_REPLICA_PAYLOAD_INVALID")]
    public void RefusesCodesThatNeedAReviewedRepair(string code)
    {
        var value = Valid(); value["ErrorCode"] = code;
        Assert.Throws<InvalidDataException>(() => HeldHeadRearmCommand.Parse(Json(value)));
    }

    [Theory]
    [InlineData("COSMIC_REPLICA_AUTHORIZATION_DENIED")]
    [InlineData("COSMIC_REPLICA_AUTHORIZATION_FAILED")]
    [InlineData("COSMIC_REPLICA_STORAGE_BINDING_INVALID")]
    public void AcceptsHostFixedCodes(string code) => Assert.True(MongoHolonReplicationOutbox.IsRearmableHeldCode(code));

    [Fact]
    public void RefusesUnknownDuplicateOrIncompleteFields()
    {
        var extra = Valid(); extra["Force"] = true;
        Assert.ThrowsAny<Exception>(() => HeldHeadRearmCommand.Parse(Json(extra)));
        var missing = Valid(); missing.Remove("TargetId");
        Assert.Throws<InvalidDataException>(() => HeldHeadRearmCommand.Parse(Json(missing)));
        var text = Encoding.UTF8.GetString(Json(Valid()));
        var duplicate = text.Replace("\"Sequence\":890", "\"Sequence\":890,\"Sequence\":1");
        Assert.Throws<InvalidDataException>(() => HeldHeadRearmCommand.Parse(Encoding.UTF8.GetBytes(duplicate)));
        var shortDigest = Valid(); shortDigest["SourcePayloadDigest"] = "abc";
        Assert.Throws<InvalidDataException>(() => HeldHeadRearmCommand.Parse(Json(shortDigest)));
    }
}
