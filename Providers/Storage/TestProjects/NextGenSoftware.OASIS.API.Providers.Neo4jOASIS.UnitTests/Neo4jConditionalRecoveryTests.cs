using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jConditionalRecoveryTests
{
    public class SessionProbe : DispatchProxy
    {
        public IRecord Record = null!;
        public int Count, Queries;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "Dispose") return null;
            if (method.Name != "RunAsync") throw new InvalidOperationException("Recovery must not open a write transaction: " + method.Name);
            var query = args![0] switch { string text => text, Query typed => typed.Text,
                _ => throw new InvalidOperationException("Unexpected query contract.") };
            Assert.IsTrue(query.StartsWith("MATCH ", StringComparison.Ordinal));
            Assert.IsFalse(query.Contains("MERGE", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(query.Contains(" SET ", StringComparison.OrdinalIgnoreCase));
            Queries++;
            var cursor = DispatchProxy.Create<IResultCursor, CursorProbe>();
            ((CursorProbe)cursor).Record = Record;
            ((CursorProbe)cursor).Count = Count;
            return Task.FromResult(cursor);
        }
    }
    public class CursorProbe : DispatchProxy
    {
        public IRecord Record = null!;
        public int Count;
        private int reads;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "FetchAsync" => Task.FromResult(reads++ < Count),
            "get_Current" => Record,
            _ => throw new InvalidOperationException("Unexpected cursor operation: " + method.Name)
        };
    }

    [DataTestMethod]
    [DataRow("committed")]
    [DataRow("absent")]
    [DataRow("preparation")]
    [DataRow("mismatch")]
    [DataRow("corrupt")]
    [DataRow("duplicate")]
    [DataRow("foreign-actor")]
    public async Task OutcomeLookupVerifiesOriginalReceiptWithoutClaimingOrMutating(string mode)
    {
        var source = new Holon { Id = Guid.NewGuid(), Name = "private source" };
        var operation = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation, actor, ProviderType.Neo4jOASIS,
            new[] { new HolonWriteCondition(source.Id, null) }), ConditionalHolonOperation.Save, new[] { source }, saveChildren: false);
        var plan = ConditionalHolonPlan.Create(request, ProviderType.Neo4jOASIS);
        var committed = new Holon { Id = source.Id, Name = source.Name, Version = 1,
            VersionId = Guid.NewGuid(), CreatedByAvatarId = actor };
        CosmicConcurrency.SetProviderRevisionId(committed, ProviderType.Neo4jOASIS, Guid.NewGuid());
        var properties = new Dictionary<string, object>
        {
            ["ResultJson"] = mode == "corrupt" ? "invalid-json" : ConditionalHolonReceipt.Write(new[] { committed }, plan),
            ["Fingerprint"] = mode == "mismatch" ? "foreign-intent" : plan.Fingerprint
        };
        if (mode == "preparation") { properties.Clear(); properties["PreparationJson"] = "{}"; }
        var node = DispatchProxy.Create<Neo4j.Driver.INode, Neo4jNativeIdentityTests.NodeProbe>();
        ((Neo4jNativeIdentityTests.NodeProbe)node).Values = properties;
        var record = DispatchProxy.Create<IRecord, Neo4jConditionalReceiptCleanupTests.RecordProbe>();
        ((Neo4jConditionalReceiptCleanupTests.RecordProbe)record).Node = node;
        var session = DispatchProxy.Create<IAsyncSession, SessionProbe>();
        var probe = (SessionProbe)session;
        probe.Record = record;
        probe.Count = mode == "absent" ? 0 : mode == "duplicate" ? 2 : 1;
        var driver = DispatchProxy.Create<IDriver, Neo4jLifecycleTests.DriverProbe>();
        var driverProbe = (Neo4jLifecycleTests.DriverProbe)driver;
        var provider = new Provider("bolt://owned.invalid", "owned", "owned", () => driver);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        driverProbe.SessionFactory = () => session;
        if (mode == "foreign-actor") request = new ConditionalHolonWriteRequest(
            new ConditionalHolonWrite(operation, Guid.NewGuid(), ProviderType.Neo4jOASIS, request.Write.Conditions),
            request.Operation, request.Roots, saveChildren: false);
        try
        {
            var result = await provider.ReadConditionalHolonWriteOutcomeAsync(request);
            var rejected = mode is "mismatch" or "corrupt" or "duplicate" or "foreign-actor";
            Assert.AreEqual(rejected, result.IsError, result.Message + result.Exception);
            Assert.IsFalse(result.IsSaved);
            Assert.IsFalse(result.IsDeleted);
            Assert.AreEqual(1, probe.Queries);
            Assert.AreEqual(0, source.Version);
            if (rejected) { Assert.IsNull(result.Result); return; }
            Assert.AreEqual(mode == "committed" ? ConditionalHolonReceiptState.Committed : ConditionalHolonReceiptState.Absent, result.Result.ReceiptState);
            Assert.AreEqual(operation, result.Result.OperationId);
            Assert.AreEqual(actor, result.Result.ActorId);
            Assert.AreEqual(ProviderType.Neo4jOASIS, result.Result.Authority);
            Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(result.Result).Contains("private source", StringComparison.Ordinal));
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
