using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    public static IEnumerable<object[]> SingleNumericHttpCases()
    {
        foreach (var provider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS })
        foreach (var operation in new[] { "Sum", "Average" })
        {
            yield return new object[] { provider, operation, false, false };
            yield return new object[] { provider, operation, true, false };
            yield return new object[] { provider, operation, true, true };
        }
    }

    [Theory]
    [MemberData(nameof(SingleNumericHttpCases))]
    public async Task SingleNumericHttpPreservesInputDomainExactWireAndReceipt(
        ProviderType provider, string operation, bool optional, bool empty)
    {
        var source = new Source(provider);
        var adapter = ScalarAdapter(source, provider);
        var domain = optional ? typeof(float?) : typeof(float);
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { domain });
        var intentOperation = operation == "Sum" ? CosmicScalarOperation.Sum : CosmicScalarOperation.Average;

        // SDK enrollment does not replace the HTTP compiler contract. Both must
        // preserve the declared Single selector before any adapter dispatch.
        adapter.SetupGet(a => a.ScalarCapabilities).Returns(new CosmicScalarCapabilities(
            Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(intentOperation, reads, new[] { domain }) }));
        const float exact = 1.0000001f;
        float? expected = empty ? (operation == "Sum" ? 0f : null) : exact;
        Type? observedType = null;
        Guid? observedActor = null;

        adapter.Setup(a => a.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, float>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, float> plan, CancellationToken _) =>
            {
                observedType = plan.Query.Selector.ValueType;
                observedActor = plan.Query.Source.ReadAccess.ActorId;
                return Task.FromResult(new OASISResult<float>(exact) { IsLoaded = true });
            });
        adapter.Setup(a => a.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, float?>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, float?> plan, CancellationToken _) =>
            {
                observedType = plan.Query.Selector.ValueType;
                observedActor = plan.Query.Source.ReadAccess.ActorId;
                return Task.FromResult(new OASISResult<float?>(expected) { IsLoaded = true });
            });

        var admitted = Admission(provider);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request
            {
                Operation = operation,
                Selection = new() { MetadataPath = new[] { "price" }, ValueType = "Single", Nullable = optional }
            }, source));

        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result = json.RootElement.GetProperty("result");
        Assert.Equal(operation, result.GetProperty("operation").GetString());
        Assert.Equal("Single", result.GetProperty("valueType").GetString());
        if (expected == null) Assert.Equal(JsonValueKind.Null, result.GetProperty("value").ValueKind);
        else Assert.Equal(expected.Value.ToString("R", CultureInfo.InvariantCulture), result.GetProperty("value").GetString());
        Assert.Equal(domain, observedType);
        Assert.Equal(Actor, observedActor);
        Assert.Single(admitted.Receipts);
        Assert.Equal(1, source.Managers);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleNumericGroupedHttpKeepsBothInputDomainsAndActor(bool optional)
    {
        var request = new CqlGroupedV1Request
        {
            Keys = new[] { new CqlProjectionV1Column { Alias = "name", Field = "Name" } },
            Columns = new[]
            {
                new CqlGroupedV1Column { Alias = "Total", Operation = "Sum", Selection = new()
                    { MetadataPath = new[] { "price" }, ValueType = "Single", Nullable = optional } },
                new CqlGroupedV1Column { Alias = "Mean", Operation = "Average", Selection = new()
                    { MetadataPath = new[] { "price" }, ValueType = "Single", Nullable = optional } }
            }
        };
        var query = CqlV1Compiler.CompileGrouped(request, Actor, ProviderType.MongoDBOASIS);
        var domain = optional ? typeof(float?) : typeof(float);
        Assert.All(query.Columns, column =>
        {
            Assert.Equal(domain, column.Selector.ValueType);
            Assert.Equal(domain, column.ValueType);
        });
        Assert.Equal(Actor, query.Source.ReadAccess.ActorId);
    }

    [Theory]
    [InlineData("Sum")]
    [InlineData("Average")]
    public async Task SingleNumericHttpReadPermissionCannotGrantArithmetic(string operation)
    {
        var provider = ProviderType.MongoDBOASIS;
        var source = new Source(provider);
        var adapter = ScalarAdapter(source, provider);
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(float) });
        var numericOperation = operation == "Sum" ? CosmicScalarOperation.Sum : CosmicScalarOperation.Average;
        adapter.SetupGet(a => a.ScalarCapabilities).Returns(new CosmicScalarCapabilities(
            Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(numericOperation, reads) }));
        var admitted = Admission(provider);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(admitted.Http,
            new CqlScalarV1Request
            {
                Operation = operation,
                Selection = new() { MetadataPath = new[] { "price" }, ValueType = "Single" }
            }, source));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Code(response));
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }

}
