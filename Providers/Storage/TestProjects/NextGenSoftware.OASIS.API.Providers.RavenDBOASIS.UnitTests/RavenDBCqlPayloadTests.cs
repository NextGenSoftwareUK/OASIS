using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS.UnitTests;

/// <summary>Controlled SDK boundary, not a native RavenDB server.</summary>
[TestClass]
public class RavenDBCqlPayloadTests
{
    public class SessionBoundary : DispatchProxy
    {
        public Dictionary<string, (string Id, bool Deleted, string? Json)> Rows = new();
        public string[] Requested = Array.Empty<string>();
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "Dispose") return null;
            if (method.Name != "LoadAsync") throw new InvalidOperationException("Unexpected SDK operation: " + method.Name);
            Requested = ((IEnumerable<string>)args![0]!).ToArray();
            ((CancellationToken)args[1]!).ThrowIfCancellationRequested();
            var dictionaryType = method.ReturnType.GenericTypeArguments[0];
            var rowType = dictionaryType.GenericTypeArguments[1];
            var result = (IDictionary)Activator.CreateInstance(dictionaryType)!;
            foreach (var key in Requested)
            {
                if (!Rows.TryGetValue(key, out var row)) { result.Add(key, null); continue; }
                var value = Activator.CreateInstance(rowType, nonPublic: true)!;
                rowType.GetProperty("Id")!.SetValue(value, row.Id);
                rowType.GetProperty("IsDeleted")!.SetValue(value, row.Deleted);
                rowType.GetProperty("DataJson")!.SetValue(value, row.Json);
                result.Add(key, value);
            }
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(dictionaryType).Invoke(null, new object[] { result });
        }
    }

    public class StoreBoundary : DispatchProxy
    {
        public IAsyncDocumentSession Session = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name != "OpenAsyncSession") throw new InvalidOperationException("Unexpected store operation: " + method.Name);
            var options = (SessionOptions)args![0]!;
            Assert.IsTrue(options.NoTracking);
            Assert.IsTrue(options.NoCaching);
            return Session;
        }
    }

    private static (RavenDBOASIS Provider, SessionBoundary Session) Provider()
    {
        var session = DispatchProxy.Create<IAsyncDocumentSession, SessionBoundary>();
        var store = DispatchProxy.Create<IDocumentStore, StoreBoundary>();
        ((StoreBoundary)(object)store).Session = session;
        var provider = new RavenDBOASIS(new[] { "http://127.0.0.1:1" });
        typeof(RavenDBOASIS).GetField("_store", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, store);
        return (provider, (SessionBoundary)(object)session);
    }

    private static CosmicQuery<Holon> Query(Guid[] ids) => CosmicQuery<Holon>.Create(
            CosmicQueryScope.Named(ProviderType.RavenDBOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.RavenDBOASIS, ids).Result).Result;

    private static CosmicQueryPlan<Holon> Plan(RavenDBOASIS provider, CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query, ProviderType.RavenDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.RavenDBOASIS, provider.QueryCapabilities) }).Result;

    private static string Payload(Guid id, bool deleted = false) => JsonSerializer.Serialize(new Holon
    {
        Id = id, Name = "Controlled record", IsActive = true,
        DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue
    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    [DataTestMethod]
    [DataRow("envelope")]
    [DataRow("payload")]
    [DataRow("deleted")]
    [DataRow("missing-json")]
    [DataRow("oversized")]
    public async Task InvalidNativeEnvelopeFailsWithoutPublishingPartialRows(string fault)
    {
        var (provider, session) = Provider();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        foreach (var id in ids) session.Rows["OasisHolons/" + id] = ("OasisHolons/" + id, false, Payload(id));
        var key = "OasisHolons/" + ids[1];
        session.Rows[key] = fault switch
        {
            "envelope" => ("OasisHolons/" + Guid.NewGuid(), false, Payload(ids[1])),
            "payload" => (key, false, Payload(Guid.NewGuid())),
            "deleted" => (key, true, Payload(ids[1])),
            "missing-json" => (key, false, null),
            _ => (key, false, new string('x', 1024 * 1024 + 1))
        };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(fault == "oversized" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task ExactCatalogueKeysOwnReadsAndDeletedMissingRowsAreExcludedBeforePaging()
    {
        var (provider, session) = Provider();
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        session.Rows["OasisHolons/" + ids[0]] = ("OasisHolons/" + ids[0], true, Payload(ids[0], true));
        foreach (var id in ids.Skip(1).Take(2)) session.Rows["OasisHolons/" + id] = ("OasisHolons/" + id, false, Payload(id));
        var query = Query(ids).Skip(1).Result.Take(1).Result;
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, query));
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { ids[2] }, result.Result.Select(row => row.Id).ToArray());
        CollectionAssert.AreEqual(ids.Select(id => "OasisHolons/" + id).ToArray(), session.Requested);
    }
}
