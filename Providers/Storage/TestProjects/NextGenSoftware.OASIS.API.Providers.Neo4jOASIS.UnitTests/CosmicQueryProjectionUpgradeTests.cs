using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryProjectionUpgradeTests
{
    public class Probe : DispatchProxy
    {
        public sealed class State
        {
            public string Mode = "";
            public int Commits, Rollbacks, Writes;
            public INode Node = null!;
        }
        public State Data = null!;
        public Dictionary<string, object>? Values;
        public List<IRecord>? Rows;
        private int position = -1;
        public static T Make<T>(State data) where T : class
        { var proxy = Create<T, Probe>(); ((Probe)(object)proxy).Data = data; return proxy; }
        private IRecord Record(Dictionary<string, object> values)
        { var row = Make<IRecord>(Data); ((Probe)row).Values = values; return row; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method!.Name)
            {
                case "Dispose": return null;
                case "AsyncSession": return Make<IAsyncSession>(Data);
                case "BeginTransactionAsync": return Task.FromResult(Make<IAsyncTransaction>(Data));
                case "CommitAsync": Data.Commits++; return Task.CompletedTask;
                case "RollbackAsync": Data.Rollbacks++; return Task.CompletedTask;
                case "get_Item": return Values![(string)args![0]!];
                case "FetchAsync": return Task.FromResult(++position < Rows!.Count);
                case "get_Current": return Rows![position];
                case "RunAsync":
                    var query = (string)args![0]!;
                    var cursor = Make<IResultCursor>(Data); var state = (Probe)cursor;
                    state.Rows = new();
                    if (query.StartsWith("MATCH (h:Holon)", StringComparison.Ordinal))
                    {
                        StringAssert.Contains(query, "LIMIT 100");
                        Assert.IsTrue(query.IndexOf("SET h.__oasisHolonLock", StringComparison.Ordinal)
                            < query.IndexOf("RETURN h", StringComparison.Ordinal));
                        if (Data.Commits == 0)
                        {
                            var projections = new List<IRelationship>();
                            if (Data.Mode is "newer" or "duplicate" or "schema")
                            {
                                var node = DispatchProxy.Create<IRelationship, Neo4jNativeIdentityTests.NodeProbe>();
                                ((Neo4jNativeIdentityTests.NodeProbe)node).Values = new Dictionary<string, object> { ["SchemaVersion"] = Data.Mode == "schema" ? (object)"1" : 2L };
                                projections.Add(node); if (Data.Mode == "duplicate") projections.Add(node);
                            }
                            state.Rows.Add(Record(new() { ["h"] = Data.Node, ["projections"] = projections }));
                        }
                    }
                    else
                    {
                        StringAssert.Contains(query, "SET p=row.projection"); Data.Writes++;
                        state.Rows.Add(Record(new() { ["count"] = Data.Mode == "ack" ? 0L : 1L }));
                    }
                    return Task.FromResult(cursor);
                default: throw new InvalidOperationException("Unexpected upgrade call: " + method.Name);
            }
        }
    }

    [DataTestMethod]
    [DataRow("ok")] [DataRow("newer")] [DataRow("duplicate")] [DataRow("ack")] [DataRow("corrupt")] [DataRow("schema")]
    public async Task UpgradeCommitsBoundedBatchesOrRejectsWithoutPublishingInvalidProjection(string mode)
    {
        var record = new Holon { Id = Guid.NewGuid(), Name = "upgrade", Description = "owned projection" };
        var node = DispatchProxy.Create<INode, Neo4jNativeIdentityTests.NodeProbe>();
        ((Neo4jNativeIdentityTests.NodeProbe)node).Values = new Dictionary<string, object> {
            ["Id"] = record.Id.ToString(), ["Name"] = record.Name, ["Description"] = record.Description,
            ["HolonType"] = record.HolonType.ToString(), ["SnapshotJson"] = mode == "corrupt" ? OasisJson.Serialize(new Holon()) : OasisJson.Serialize(record) };
        var state = new Probe.State { Mode = mode, Node = node };
        var provider = new Neo4jOASIS("bolt://owned.invalid", "owned", "owned", () => Probe.Make<IDriver>(state));
        if (mode == "ok")
        {
            await provider.UpgradeCqlProjectionsAsync();
            Assert.AreEqual(2, state.Commits); Assert.AreEqual(1, state.Writes); // One data batch and empty completion.
            Assert.AreEqual(0, state.Rollbacks);
        }
        else
        {
            await Assert.ThrowsExceptionAsync<System.IO.InvalidDataException>(() => provider.UpgradeCqlProjectionsAsync());
            Assert.AreEqual(0, state.Commits); Assert.AreEqual(1, state.Rollbacks);
            Assert.AreEqual(mode == "ack" ? 1 : 0, state.Writes);
        }
    }
}
