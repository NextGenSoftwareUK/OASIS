using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Grpc;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class GrpcHolonWriteContractTests
{
    [Fact]
    public void ProtobufAndRecursiveHolonContractsRetainChildrenConditionsAndExactMetadata()
    {
        var id = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var wire = new SaveHolonGrpcRequest
        {
            OperationId = Guid.NewGuid().ToString("D"), ProviderType = "Neo4jOASIS",
            SaveChildren = true, Recursive = true, MaxChildDepth = 2,
            HolonJson = $$"""
            {"id":"{{id}}","name":"root","childRoleManifestJson":"stored-observation",
             "metaData":{"largeInteger":9007199254740993,"fraction":0.1234567890123456789},
             "children":[{"id":"{{childId}}","name":"child","childRoleManifestJson":"child-observation"}]}
            """
        };
        wire.ExpectedRevisions.Add(id.ToString("D"), Guid.Empty.ToString("D"));
        wire.ExpectedRevisions.Add(childId.ToString("D"), "");

        var decoded = GrpcHolonWriteContract.Decode(SaveHolonGrpcRequest.Parser.ParseFrom(wire.ToByteArray()));

        Assert.False(decoded.IsError, decoded.Message);
        Assert.Equal(Guid.Parse(wire.OperationId), decoded.Result.OperationId);
        Assert.Equal(Guid.Empty, decoded.Result.ExpectedRevisions[id]);
        Assert.Null(decoded.Result.ExpectedRevisions[childId]);
        Assert.Equal(2, decoded.Result.MaxChildDepth);
        Assert.True(decoded.Result.Recursive);
        Assert.True(decoded.Result.SaveChildren);
        Assert.Equal("9007199254740993", decoded.Result.Holon.MetaData["largeInteger"].ToString());
        Assert.Equal("0.1234567890123456789", decoded.Result.Holon.MetaData["fraction"].ToString());
        Assert.Equal("stored-observation", decoded.Result.Holon.ChildRoleManifestJson);
        Assert.Equal(childId, Assert.Single(decoded.Result.Holon.Children).Id);
        Assert.Equal("child-observation", Assert.IsType<Holon>(Assert.Single(decoded.Result.Holon.Children)).ChildRoleManifestJson);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"id\":\"not-a-guid\"}")]
    public void MalformedHolonsRejectInsteadOfBecomingNewCreates(string json)
    {
        var decoded = GrpcHolonWriteContract.Decode(new SaveHolonGrpcRequest { HolonJson = json });
        Assert.True(decoded.IsError);
        Assert.Equal("COSMIC_WRITE_INTENT_INVALID", decoded.ErrorCode);
    }

    [Fact]
    public void AmbiguousLegacyAndV2PayloadsReject()
    {
        var decoded = GrpcHolonWriteContract.Decode(new SaveHolonGrpcRequest
            { Holon = new HolonMessage { Name = "other" }, HolonJson = "{\"name\":\"first\"}" });
        Assert.True(decoded.IsError);
    }

    [Fact]
    public void CanonicallyDuplicateRevisionKeysReject()
    {
        const string id = "abcdef01-abcd-abcd-abcd-abcdef012345";
        var wire = new SaveHolonGrpcRequest { HolonJson = "{}" };
        wire.ExpectedRevisions.Add(id, "");
        wire.ExpectedRevisions.Add(id.ToUpperInvariant(), "");
        Assert.True(GrpcHolonWriteContract.Decode(wire).IsError);
    }

    [Fact]
    public void CrossProviderChildFlagCannotSilentlyChangeConditionalAuthority()
    {
        var wire = new SaveHolonGrpcRequest { HolonJson = "{}", SaveChildrenForAllProviders = true };
        var result = GrpcHolonWriteContract.Decode(wire);
        Assert.True(result.IsError);
        Assert.Contains("replication enrollment", result.Message);
    }

    [Fact]
    public void InvalidRevisionCannotBecomeCreateOnly()
    {
        var wire = new SaveHolonGrpcRequest { HolonJson = "{}" };
        wire.ExpectedRevisions.Add(Guid.NewGuid().ToString("D"), "not-a-provider-token");
        Assert.True(GrpcHolonWriteContract.Decode(wire).IsError);
    }

    [Fact]
    public void AcknowledgementRetainsCommittedDeltaAndProviderEvidence()
    {
        var result = new OASISResult<IHolon>(new Holon { Id = Guid.NewGuid(), Name = "committed" })
        {
            IsSaved = true,
            MetaData = new Dictionary<string, string> { ["COSMIC.CommittedDelta"] = "{\"format\":1}", ["receipt"] = "original" }
        };
        var response = HolonResponse.Parser.ParseFrom(GrpcHolonWriteContract.Encode(result).ToByteArray());
        Assert.True(response.IsSaved);
        Assert.Equal("{\"format\":1}", response.CommittedDeltaJson);
        Assert.Equal("original", response.ResultMetadata["receipt"]);
        Assert.Contains("committed", response.HolonJson);

        var failure = GrpcHolonWriteContract.Encode(new OASISResult<IHolon>
            { IsError = true, ErrorCount = 3, ErrorCode = "COSMIC_CONCURRENCY_CONFLICT" });
        Assert.Equal(3, failure.ErrorCount);
        Assert.Equal("COSMIC_CONCURRENCY_CONFLICT", failure.ErrorCode);
        Assert.Empty(failure.HolonJson);
    }
}
