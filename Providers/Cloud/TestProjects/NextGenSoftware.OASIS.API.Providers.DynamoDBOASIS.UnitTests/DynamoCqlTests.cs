using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids, ProviderType authority = ProviderType.DynamoDBOASIS)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, ids).Result).Result;

    [TestMethod]
    public void NativeKeysIntersectFrozenBusinessAndAuthoritativeSelections()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }; var first = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var captured = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id));
        Assert.IsFalse(captured.IsError, captured.Message); var query = captured.Result;
        selected[0] = Guid.NewGuid(); ids[0] = Guid.NewGuid();
        var result = DynamoCqlCompiler.Compile(query, ProviderType.DynamoDBOASIS);
        Assert.IsFalse(result.IsError, result.Message); CollectionAssert.AreEqual(new[] { first }, result.Result.ToArray());
        Assert.AreEqual(0, DynamoCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.DynamoDBOASIS).Result.Count);
        Assert.IsTrue(DynamoCqlCompiler.Compile(Query(ids), ProviderType.AWSOASIS).IsError);
    }

    [DataTestMethod]
    [DataRow("name")] [DataRow("metadata")] [DataRow("order")] [DataRow("graph")] [DataRow("bound")]
    public void UnsupportedShapesRejectBeforeNativeDispatch(string kind)
    {
        var query = Query(Enumerable.Range(0, kind == "bound" ? 101 : 2).Select(_ => Guid.NewGuid()).ToArray());
        query = kind switch {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query };
        var rejected = DynamoCqlCompiler.Compile(query, ProviderType.DynamoDBOASIS);
        Assert.IsTrue(rejected.IsError); Assert.IsNull(rejected.Result);
    }

    private sealed class Probe(int shape) : AmazonDynamoDBClient(new BasicAWSCredentials("owned", "owned"),
        new AmazonDynamoDBConfig { ServiceURL = "http://127.0.0.1:18001", AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 })
    {
        public int Batches;
        public override Task<DescribeTableResponse> DescribeTableAsync(string name, CancellationToken token = default)
            => Task.FromResult(new DescribeTableResponse { HttpStatusCode = HttpStatusCode.OK, Table = new() {
                TableName = name, TableStatus = TableStatus.ACTIVE, KeySchema = new() { new() { AttributeName = "id", KeyType = KeyType.HASH } },
                AttributeDefinitions = new() { new() { AttributeName = "id", AttributeType = ScalarAttributeType.S } } } });
        public override Task<BatchGetItemResponse> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken token = default)
        {
            Batches++; Assert.AreEqual(1, request.RequestItems.Count); Assert.IsTrue(request.RequestItems["OasisHolons"].ConsistentRead);
            var keys = request.RequestItems["OasisHolons"].Keys;
            var rows = keys.AsEnumerable().Reverse().Select(k => {
                var id = shape == 3 ? Guid.NewGuid() : Guid.Parse(k["id"].S);
                var row = new Holon { Id = id, Name = "coffee", DeletedDate = shape == 6 ? new DateTime(2026,10,29,0,0,0,DateTimeKind.Utc) : DateTime.MinValue };
                return new Dictionary<string, AttributeValue> { ["id"] = new() { S = id.ToString("D") },
                    ["data_json"] = new() { S = shape == 5 ? "{invalid" : OasisJson.Serialize(row) } };
            }).ToList();
            if (shape == 4) rows.Add(rows[0]);
            return Task.FromResult(new BatchGetItemResponse { HttpStatusCode = shape == 1 ? HttpStatusCode.BadGateway : HttpStatusCode.OK,
                Responses = new() { ["OasisHolons"] = rows },
                UnprocessedKeys = shape == 2 ? new() { ["OasisHolons"] = new() { Keys = keys } } : new() });
        }
    }

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)] [DataRow(6)]
    public async Task OneNativeBatchRequiresCompleteValidSelectionWithoutPartialPublication(int shape)
    {
        var provider = new DynamoDBOASIS("owned", "owned", "us-east-1");
        var field = typeof(DynamoDBOASIS).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (AmazonDynamoDBClient)field.GetValue(provider)!; using var probe = new Probe(shape); field.SetValue(provider, probe);
        try
        {
            var activated = await provider.ActivateProviderAsync(); Assert.IsFalse(activated.IsError, activated.Message);
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }; var query = Query(ids).Where(h => ids.Contains(h.Id)).Result;
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.DynamoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.DynamoDBOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);
            var result = await provider.ExecuteCosmicQueryAsync(planned.Result);
            Assert.AreEqual(1, probe.Batches); Assert.AreEqual(shape is not (0 or 6), result.IsError, result.Message + result.Exception);
            if (result.IsError) Assert.IsNull(result.Result);
            else Assert.AreEqual(shape == 6 ? 0 : 2, result.Result.Count);
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            Assert.AreEqual("CQL_QUERY_CANCELLED", (await provider.ExecuteCosmicQueryAsync(planned.Result, cancellationToken: cancelled.Token)).ErrorCode);
            Assert.AreEqual(1, probe.Batches);
        }
        finally { field.SetValue(provider, original); await provider.DeActivateProviderAsync(); }
    }
}
