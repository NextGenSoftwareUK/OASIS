using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Fact]
    public async Task RowRequestRetainsMetadataDisclosureAcrossProviderAwait()
    {
        var source = new Source(ProviderType.MongoDBOASIS);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var native = new TaskCompletionSource<OASISResult<IReadOnlyList<IHolon>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        source.Adapter.Setup(value => value.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.SetResult(); return native.Task; });
        var request = new CqlV1Request { IncludeMetadata = false };
        var admitted = Admission(ProviderType.MongoDBOASIS);
        var execution = admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, request, source));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        request.IncludeMetadata = true;
        native.SetResult(new(new IHolon[] { new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Actor,
            Name = "owned", MetaData = new() { ["note"] = "not requested" } } }));
        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(await execution).FileContents);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("result").GetProperty("rows")[0]
            .GetProperty("metadata").ValueKind);
    }

    [Fact]
    public void CapturedNestedIntentSurvivesCallerMutationAndJsonDocumentDisposal()
    {
        var document = JsonDocument.Parse("\"coffee\"");
        var filter = new CqlV1Filter { Field = "Name", Value = document.RootElement };
        var child = new CqlV1Filter { MetadataPath = new[] { "flavour" }, Value = document.RootElement };
        var request = new CqlV1Request { Filters = new[] { filter },
            AnyChildren = new[] { new CqlV1ChildExists { Filters = new[] { child } } } };
        var captured = CqlRequestCapture.Capture(request);
        document.Dispose();
        filter.Field = "not-an-admitted-field";
        child.MetadataPath![0] = "";
        request.AnyChildren[0].Filters = Array.Empty<CqlV1Filter>();
        var query = CqlV1Compiler.Compile(captured, Actor, ProviderType.MongoDBOASIS);
        Assert.Equal(3, query.Predicates.Count);
    }

    [Fact]
    public void CapturedExistencePredicatePreservesAbsentValueContract()
    {
        var request = new CqlV1Request { Filters = new[] { new CqlV1Filter
            { MetadataPath = new[] { "note" }, Operator = CqlV1Operator.Exists } } };
        var captured = CqlRequestCapture.Capture(request);
        var query = CqlV1Compiler.Compile(captured, Actor, ProviderType.MongoDBOASIS);
        Assert.Equal(2, query.Predicates.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FederationRequestRetainsAdmittedDisclosureAndWireLimitAcrossProviderAwait(bool wireLimit)
    {
        var sources = new FederationSources();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var native = new TaskCompletionSource<OASISResult<IReadOnlyList<IHolon>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        sources.Items[ProviderType.MongoDBOASIS].Adapter.Setup(value => value.ExecuteCosmicQueryAsync(
                It.IsAny<CosmicQueryPlan<Holon>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => { entered.SetResult(); return native.Task; });
        var request = new CqlFederationV1Request
        {
            MaximumOutputBytes = wireLimit ? 250 : 1048576,
            Source = new CqlV1Request { IncludeMetadata = false }
        };
        var admitted = FederationAdmission();
        var execution = admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, request, sources));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // A caller still owns its DTO. Mutation during real suspension must not
        // change the operation whose immutable intent was already admitted.
        request.Source.IncludeMetadata = true;
        request.MaximumOutputBytes = 1048576;
        native.SetResult(new OASISResult<IReadOnlyList<IHolon>>(wireLimit ? Array.Empty<IHolon>() : new IHolon[]
        { new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Actor, Name = "owned", MetaData = new() { ["note"] = "not requested" } } }));
        var result = await execution;
        if (wireLimit)
        {
            Assert.Equal("CQL_FEDERATION_OUTPUT_LIMIT", Code(result));
            Assert.Equal(413, Assert.IsType<ObjectResult>(result).StatusCode);
        }
        else
        {
            using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
            Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("result").GetProperty("rows")[0]
                .GetProperty("selected").GetProperty("row").GetProperty("metadata").ValueKind);
        }
        Assert.Single(admitted.Receipts);
    }
}
