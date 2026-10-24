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
    private static CqlGroupedV1Request GroupRequest() => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "name", Field = "Name" } },
        Columns = new[]
        {
            new CqlGroupedV1Column { Alias = "Name", KeyAlias = "name" },
            new CqlGroupedV1Column { Alias = "Stock", Operation = "Sum", Selection = new CqlScalarV1Selection
                { MetadataPath = new[] { "stock" }, ValueType = "Int64" } },
            new CqlGroupedV1Column { Alias = "Mean", Operation = "Average", Selection = new CqlScalarV1Selection { Field = "Version" } }
        }
    };

    private static void GroupAdapter(Source source, ProviderType provider, IReadOnlyList<CosmicGroupedRow> rows)
    {
        var projected = new CosmicProjectionCapabilities(new[]
        {
            new CosmicProjectionFieldCapability("Name", typeof(string)),
            new CosmicProjectionFieldCapability("Version", typeof(int))
        }, new[] { typeof(long) });
        var scalar = new CosmicScalarCapabilities(new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount }, new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, projected),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, projected)
        });
        var adapter = source.Storage.As<ICosmicQueryGroupedStorageProvider>();
        adapter.SetupGet(a => a.QueryProviderType).Returns(provider);
        adapter.SetupGet(a => a.QueryCapabilities).Returns(Capabilities);
        adapter.SetupGet(a => a.GroupedCapabilities).Returns(new CosmicGroupedCapabilities(projected, scalar));
        adapter.Setup(a => a.ExecuteCosmicGroupedAsync(
            It.IsAny<CosmicGroupedPlan<Holon, CosmicProjectionRecord, CosmicProjectionRecord>>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<CosmicGroupedRow>>(rows) { IsLoaded = true });
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task GroupedHttpRetainsExactWireDomainsAndOneDurableAttempt(ProviderType provider)
    {
        var source = new Source(provider);
        GroupAdapter(source, provider, new[] { new CosmicGroupedRow(2, new object[] { "coffee" }, "coffee", 9007199254740993L, 1.5m) });
        var admitted = Admission(provider);
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteGroupedAsync(admitted.Http, GroupRequest(), source));
        using var document = JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result = document.RootElement.GetProperty("result");
        Assert.Equal("cql-grouped-v1", result.GetProperty("version").GetString());
        Assert.Equal(provider.ToString(), result.GetProperty("provider").GetString());
        var row = result.GetProperty("rows")[0];
        Assert.Equal("coffee", row[0].GetString()); Assert.Equal("9007199254740993", row[1].GetString()); Assert.Equal("1.5", row[2].GetString());
        Assert.Equal("Int64", result.GetProperty("columns")[1].GetProperty("valueType").GetString());
        Assert.Equal("Decimal", result.GetProperty("columns")[2].GetProperty("valueType").GetString());
        Assert.Equal(1, Assert.Single(admitted.Receipts).Units); Assert.Null(source.Observed);
    }

    [Fact]
    public async Task MalformedGroupedRequestDoesNotResolveOrDispatchAProvider()
    {
        var source = new Source(ProviderType.MongoDBOASIS); var admitted = Admission(ProviderType.MongoDBOASIS);
        var request = GroupRequest(); request.MaximumGroups = 0;
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteGroupedAsync(admitted.Http, request, source));
        Assert.Equal(400, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal(0, source.Lookups); Assert.Equal(0, source.Managers);
    }
}
