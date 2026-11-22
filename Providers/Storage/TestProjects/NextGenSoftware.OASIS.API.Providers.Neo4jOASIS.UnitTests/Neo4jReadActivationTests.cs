using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jReadActivationTests
{
    public class ReadSession : DispatchProxy
    {
        public bool Invalid;
        public int Checks;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "Dispose") return null;
            if (method.Name == "BeginTransactionAsync")
            {
                var transaction = DispatchProxy.Create<IAsyncTransaction, ReadTransaction>();
                ((ReadTransaction)transaction).Owner = this;
                return Task.FromResult(transaction);
            }
            throw new InvalidOperationException("Unexpected read session call: " + method.Name);
        }
    }

    public class ReadTransaction : DispatchProxy
    {
        public ReadSession Owner = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "Dispose") return null;
            if (method.Name is "CommitAsync" or "RollbackAsync") return Task.CompletedTask;
            if (method.Name == "RunAsync")
            {
                var query = (string)args![0]!;
                Assert.IsTrue(query.StartsWith("MATCH (h:Holon)", StringComparison.Ordinal));
                Assert.IsTrue(query.Contains("RETURN 1 AS invalid LIMIT 1", StringComparison.Ordinal));
                foreach (var mutation in new[] { "SET ", "REMOVE ", "MERGE ", "CREATE ", "DELETE " })
                    Assert.IsFalse(query.Contains(mutation, StringComparison.Ordinal), query);
                Owner.Checks++;
                var cursor = DispatchProxy.Create<IResultCursor, Neo4jLifecycleTests.SchemaCursorProbe>();
                ((Neo4jLifecycleTests.SchemaCursorProbe)cursor).Empty = !Owner.Invalid;
                return Task.FromResult(cursor);
            }
            throw new InvalidOperationException("Unexpected read transaction call: " + method.Name);
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ReadOnlyActivationNeverBootstrapsOrRepairsStorage(bool invalidProjection)
    {
        var driver = DispatchProxy.Create<IDriver, Neo4jLifecycleTests.DriverProbe>();
        var probe = (Neo4jLifecycleTests.DriverProbe)driver;
        var session = DispatchProxy.Create<IAsyncSession, ReadSession>();
        var reads = (ReadSession)session;
        reads.Invalid = invalidProjection;
        probe.SessionFactory = () => session;
        var provider = new Provider("bolt://127.0.0.1:17687", "owned", "owned", () => driver);
        try
        {
            var result = await provider.ActivateQuerySourceAsync();
            Assert.AreEqual(invalidProjection, result.IsError);
            Assert.AreEqual(!invalidProjection, provider.IsQuerySourceActivated);
            Assert.IsFalse(provider.IsProviderActivated, "Read enrollment cannot grant storage activation.");
            Assert.AreEqual(1, reads.Checks);
            Assert.AreEqual(0, probe.SchemaRuns);
            Assert.AreEqual(0, probe.SchemaCreates);
            if (invalidProjection) Assert.AreEqual("CQL_SCHEMA_UPGRADE_REQUIRED", result.ErrorCode);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        Assert.IsFalse(provider.IsQuerySourceActivated);
    }

    [TestMethod]
    public async Task ReadOnlyVerificationFailureDoesNotPublishReadiness()
    {
        var driver = DispatchProxy.Create<IDriver, Neo4jLifecycleTests.DriverProbe>();
        var probe = (Neo4jLifecycleTests.DriverProbe)driver;
        var cause = new IOException("owned read-only verification failure");
        probe.VerifyFailure = cause;
        var provider = new Provider("bolt://127.0.0.1:17687", "owned", "owned", () => driver);
        try
        {
            var result = await provider.ActivateQuerySourceAsync();
            Assert.IsTrue(result.IsError);
            Assert.AreSame(cause, result.Exception);
            Assert.IsFalse(provider.IsQuerySourceActivated);
            Assert.IsFalse(provider.IsProviderActivated);
            Assert.AreEqual(0, probe.SchemaCreates);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
