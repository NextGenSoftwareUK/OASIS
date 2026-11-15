extern alias ShippingWeb4;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using CqlRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Request;
using CqlFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Filter;
using CqlOperator = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Operator;
using CqlScalar = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Scalar;
using CqlCompiler = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1Compiler;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyMetadataPresenceWire(ICosmicQueryStorageProvider adapter,
        Guid rootId, List<object> checks)
    {
        var provider = adapter.QueryProviderType;
        var target = new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) };
        var id = new CqlFilter
        {
            Field = "Id", Operator = CqlOperator.Equal, Value = JsonSerializer.SerializeToElement(rootId)
        };
        var actor = Guid.NewGuid();

        async Task AssertMatch(string key, CqlOperator operation, bool matches, string? integer = null)
        {
            var filter = new CqlFilter { MetadataPath = new[] { key }, Operator = operation };
            if (integer != null)
            {
                filter.Scalar = CqlScalar.Integer;
                filter.Value = JsonSerializer.SerializeToElement(integer);
            }

            var request = new CqlRequest { Filters = new[] { id, filter }, OrderBy = "Id", Take = 2 };
            var query = CqlCompiler.Compile(request, actor, provider);
            var plan = CosmicQueryPlanner.Plan(query, provider, target);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var expected = matches ? new[] { rootId } : Array.Empty<Guid>();
            CollectionAssert.AreEqual(expected, result.Result.Providers.Single().Rows.Select(row => row.Id).ToArray());
        }

        await AssertMatch("missing", CqlOperator.IsMissing, true);
        await AssertMatch("presentNull", CqlOperator.IsMissing, false);
        await AssertMatch("text", CqlOperator.IsNotNull, true);
        await AssertMatch("presentNull", CqlOperator.IsNotNull, false);
        await AssertMatch("missing", CqlOperator.IsNotNull, false);
        await AssertMatch("i64", CqlOperator.Equal, true, "9007199254740993");
        await AssertMatch("i64", CqlOperator.Equal, false, "9007199254740992");
        checks.Add(new { provider = provider.ToString(), metadataWire = "missing, null, populated and exact signed Int64" });
    }
}
