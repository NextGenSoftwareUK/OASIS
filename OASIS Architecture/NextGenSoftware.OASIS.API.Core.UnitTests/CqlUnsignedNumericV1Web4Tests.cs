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
    [Theory]
    [InlineData(false, 0UL)]
    [InlineData(false, ulong.MaxValue)]
    [InlineData(true, 0UL)]
    [InlineData(true, ulong.MaxValue)]
    public async Task UnsignedNumericSumHttpRetainsExactWireDomainAndReceipt(bool optional, ulong value)
    {
        var provider = ProviderType.MongoDBOASIS;
        var source = new Source(provider);
        var scalar = ScalarAdapter(source, provider);
        var domain = optional ? typeof(ulong?) : typeof(ulong);
        var reads = new CosmicProjectionCapabilities(
            Array.Empty<CosmicProjectionFieldCapability>(), new[] { domain });
        scalar.SetupGet(adapter => adapter.ScalarCapabilities).Returns(new CosmicScalarCapabilities(
            Array.Empty<CosmicScalarOperation>(), new[]
            {
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reads, new[] { domain })
            }));

        Type? observed = null;
        scalar.Setup(adapter => adapter.ExecuteCosmicScalarAsync(
                It.IsAny<CosmicScalarPlan<Holon, ulong>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, ulong> plan, CancellationToken token) =>
            {
                observed = plan.Query.Selector.ValueType;
                Assert.Equal(Actor, plan.Query.Source.ReadAccess.ActorId);
                return Task.FromResult(new OASISResult<ulong>(value) { IsLoaded = true });
            });
        scalar.Setup(adapter => adapter.ExecuteCosmicScalarAsync(
                It.IsAny<CosmicScalarPlan<Holon, ulong?>>(), It.IsAny<CancellationToken>()))
            .Returns((CosmicScalarPlan<Holon, ulong?> plan, CancellationToken token) =>
            {
                observed = plan.Query.Selector.ValueType;
                Assert.Equal(Actor, plan.Query.Source.ReadAccess.ActorId);
                return Task.FromResult(new OASISResult<ulong?>(value) { IsLoaded = true });
            });

        var admission = Admission(provider);
        var response = await admission.Policy.RunAsync(() => CqlV1Endpoint.ExecuteScalarAsync(
            admission.Http, new CqlScalarV1Request
            {
                Operation = "Sum",
                Selection = new()
                {
                    MetadataPath = new[] { "counter" },
                    ValueType = "UInt64",
                    Nullable = optional
                }
            }, source));

        using var json = JsonDocument.Parse(Assert.IsType<FileContentResult>(response).FileContents);
        var result = json.RootElement.GetProperty("result");
        Assert.Equal("UInt64", result.GetProperty("valueType").GetString());
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), result.GetProperty("value").GetString());
        Assert.Equal(domain, observed);
        Assert.Single(admission.Receipts);
        Assert.Equal(1, source.Managers);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnsignedNumericGroupedHttpCompilerPreservesInputAndResultDomains(bool optional)
    {
        var request = new CqlGroupedV1Request
        {
            Keys = new[] { new CqlProjectionV1Column { Alias = "name", Field = "Name" } },
            Columns = new[]
            {
                new CqlGroupedV1Column
                {
                    Alias = "Total", Operation = "Sum",
                    Selection = new() { MetadataPath = new[] { "counter" }, ValueType = "UInt64", Nullable = optional }
                },
                new CqlGroupedV1Column
                {
                    Alias = "Mean", Operation = "Average",
                    Selection = new() { MetadataPath = new[] { "counter" }, ValueType = "UInt64", Nullable = optional }
                }
            }
        };

        var query = CqlV1Compiler.CompileGrouped(request, Actor, ProviderType.MongoDBOASIS);
        var input = optional ? typeof(ulong?) : typeof(ulong);
        Assert.Equal(input, query.Columns[0].Selector.ValueType);
        Assert.Equal(input, query.Columns[0].ValueType);
        Assert.Equal(input, query.Columns[1].Selector.ValueType);
        Assert.Equal(optional ? typeof(decimal?) : typeof(decimal), query.Columns[1].ValueType);
        Assert.Equal(Actor, query.Grouping.Source.ReadAccess.ActorId);
    }

}
