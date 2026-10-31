using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlMembershipCaptureTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void MembershipSnapshotOwnsItsLiteralsAfterRequestMutationAndDocumentDisposal(int count)
    {
        CqlV1Request captured;
        using (var document = JsonDocument.Parse(JsonSerializer.Serialize(Enumerable.Range(1, count).Select(i => i.ToString()))))
        {
            var filter = new CqlV1Filter { Field = "Name", Operator = CqlV1Operator.In, Value = document.RootElement };
            var request = new CqlV1Request { Filters = new[] { filter } };
            captured = CqlRequestCapture.Capture(request);
            filter.Value = JsonSerializer.SerializeToElement(new[] { "changed" });
            request.Filters = Array.Empty<CqlV1Filter>();
        }

        var query = CqlV1Compiler.Compile(captured, Guid.NewGuid(), ProviderType.MongoDBOASIS);
        var membership = Assert.IsType<CosmicQueryMembership>(Assert.Single(query.BusinessPredicates));
        Assert.Equal(count, membership.Values.Count);
        Assert.Equal("1", membership.Values[0].Value);
        Assert.Equal(count.ToString(), membership.Values[^1].Value);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[null]")]
    [InlineData("[[1]]")]
    [InlineData("[{}]")]
    [InlineData("\"one\"")]
    public void SnapshotRejectsUnboundedOrNonScalarMembership(string json)
    {
        using var document = JsonDocument.Parse(json);
        var request = new CqlV1Request { Filters = new[] {
            new CqlV1Filter { Field = "Name", Operator = CqlV1Operator.In, Value = document.RootElement }
        } };
        Assert.Throws<ArgumentException>(() => CqlRequestCapture.Capture(request));
    }

    [Theory]
    [InlineData("members")]
    [InlineData("string")]
    [InlineData("number")]
    public void SnapshotBoundsAllocationBeforeLiteralCompilation(string scenario)
    {
        var json = scenario switch
        {
            "members" => JsonSerializer.Serialize(Enumerable.Repeat("key", 101)),
            "string" => JsonSerializer.Serialize(new[] { new string('x', 513) }),
            _ => "[" + new string('9', 65) + "]"
        };
        using var document = JsonDocument.Parse(json);
        var request = new CqlV1Request { AnyOf = new[] {
            new CqlV1Filter { Field = "Name", Operator = CqlV1Operator.In, Value = document.RootElement }
        } };
        Assert.Throws<ArgumentException>(() => CqlRequestCapture.Capture(request));
    }

    [Fact]
    public void ScalarOperatorsStillCannotCaptureArrays()
    {
        var request = new CqlV1Request { Filters = new[] {
            new CqlV1Filter { Field = "Name", Operator = CqlV1Operator.Equal,
                Value = JsonSerializer.SerializeToElement(new[] { "one" }) }
        } };
        Assert.Throws<ArgumentException>(() => CqlRequestCapture.Capture(request));
    }

    [Fact]
    public void PublishedActivityPubPresetsTraverseTheHttpSnapshotBeforeNativePlanning()
    {
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "cql-activitypub-public-examples.json")));
        foreach (var example in document.RootElement.GetProperty("ActivityPubOASIS").EnumerateArray())
        {
            var request = example.GetProperty("request").Deserialize<CqlV1Request>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            var captured = CqlRequestCapture.Capture(request);
            var query = CqlV1Compiler.Compile(captured, Guid.NewGuid(), ProviderType.ActivityPubOASIS);
            var capabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup("MastodonStatusId", 128, 40);
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.ActivityPubOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ActivityPubOASIS, capabilities) });
            Assert.False(planned.IsError, planned.Message);
            Assert.Equal(example.GetProperty("id").GetString() == "public-posts" ? 3 : 2,
                capabilities.StringKeySetLookup.SelectKeys(planned.Result.Query).Result.Count);
        }
    }
}
