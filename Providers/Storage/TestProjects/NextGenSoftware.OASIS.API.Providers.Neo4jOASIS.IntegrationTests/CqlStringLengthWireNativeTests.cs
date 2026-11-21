extern alias ShippingWeb4;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using CqlRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Request;
using CqlFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Filter;
using CqlOperator = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Operator;
using CqlFunction = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Function;
using CqlCompiler = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1Compiler;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyNativeStringLengthWire(
        ICosmicQueryStorageProvider adapter, Guid[] ids, List<object> checks)
    {
        var storage = (IOASISStorageProvider)adapter;
        var root = await storage.LoadHolonAsync(ids[0], loadChildren: false, recursive: false);
        var branch = await storage.LoadHolonAsync(ids[2], loadChildren: false, recursive: false);
        Assert.IsFalse(root.IsError, root.Message);
        Assert.IsFalse(branch.IsError, branch.Message);
        Assert.IsFalse(branch.Result.IsPublic);
        var rootOwner = root.Result.CreatedByAvatarId;
        var branchOwner = branch.Result.CreatedByAvatarId;
        Assert.AreNotEqual(rootOwner, branchOwner);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var provider = adapter.QueryProviderType;
        var targets = new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) };

        async Task<Guid[]> Read(Guid actor, bool metadata, string bound)
        {
            var request = new CqlRequest
            {
                OrderBy = "Id", Take = 10,
                Filters = new[]
                {
                    new CqlFilter { Field = "Id", Operator = CqlOperator.In,
                        Value = JsonSerializer.SerializeToElement(ids) },
                    new CqlFilter
                    {
                        Field = metadata ? null : "Name",
                        MetadataPath = metadata ? new[] { "lengthValue" } : null,
                        Function = CqlFunction.Utf16Length,
                        Operator = CqlOperator.GreaterThanOrEqual,
                        Value = JsonSerializer.SerializeToElement(bound)
                    }
                }
            };
            var captured = JsonSerializer.Deserialize<CqlRequest>(JsonSerializer.Serialize(request, options), options)!;
            var access = CosmicQueryReadAccess.OwnerOrPublic(actor);
            Assert.IsFalse(access.IsError, access.Message);
            var source = CqlCompiler.Compile(captured, actor, provider, access.Result);
            var plan = CosmicQueryPlanner.Plan(source, provider, targets);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            return result.Result.Providers.Single().Rows.Select(row => row.Id).ToArray();
        }

        foreach (var actor in new[] { rootOwner, branchOwner, Guid.NewGuid() })
        {
            var expected = actor == branchOwner ? ids : ids.Where(id => id != ids[2]).ToArray();
            foreach (var metadata in new[] { false, true })
            {
                var watch = Stopwatch.StartNew();
                var actual = await Read(actor, metadata, "0");
                CollectionAssert.AreEquivalent(expected, actual);
                checks.Add(new { provider = provider.ToString(), utf16Wire = "compiled API owner/public scope",
                    metadata, privateBranchVisible = actor == branchOwner, elapsedMilliseconds = watch.ElapsedMilliseconds });
            }
        }
        var precise = await Read(rootOwner, metadata: true, "9223372036854775807");
        Assert.AreEqual(0, precise.Length);
        checks.Add(new { provider = provider.ToString(), utf16Wire = "exact Int64 bound" });
    }
}
