using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS.IntegrationTests;

/// <summary>Read-only qualification of an explicitly configured, already seeded native source.</summary>
[TestClass]
public class RavenDBCqlNativeTests
{
    public sealed class Envelope
    {
        public string? Id { get; set; }
        public bool IsDeleted { get; set; }
        public string? DataJson { get; set; }
    }

    public sealed class Source
    {
        public string Endpoint { get; set; } = "";
        public string Database { get; set; } = "";
        public Guid Actor { get; set; }
        public Guid[] LiveIds { get; set; } = Array.Empty<Guid>();
        public Guid DeletedId { get; set; }
        public Guid MissingId { get; set; }
    }

    [TestMethod]
    [TestCategory("RavenDBCqlNative")]
    public async Task RetainedNativeCatalogueSupportsExactThreeTwoAndExcludesDeletedAndMissing()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_RAVENDB_CQL_SOURCE");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path), "Explicit retained-source configuration is required; no auto-provisioning.");
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllTextAsync(path!))!;
        Assert.IsNotNull(source);
        Assert.IsTrue(Uri.TryCreate(source.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == "http" && endpoint.IsLoopback && endpoint.UserInfo.Length == 0
            && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0,
            "This local fixture requires an explicit HTTP loopback service.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(source.Database));
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.AreEqual(3, source.LiveIds.Length);
        var catalogue = source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).ToArray();
        Assert.AreEqual(5, catalogue.Distinct().Count());
        Assert.IsFalse(catalogue.Contains(Guid.Empty));
        using var store = new DocumentStore { Urls = new[] { source.Endpoint }, Database = source.Database };
        store.Initialize();
        async Task<string> ObserveSource()
        {
            using var session = store.OpenAsyncSession(new SessionOptions { NoTracking = true, NoCaching = true });
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var keys = catalogue.Select(id => "OasisHolons/" + id.ToString("D")).ToArray();
            var documents = await session.LoadAsync<Envelope>(keys, timeout.Token);
            Assert.IsNotNull(documents[keys[3]], "Deleted control must exist natively, not merely be absent.");
            Assert.IsTrue(documents[keys[3]].IsDeleted);
            Assert.IsNull(documents[keys[4]], "Missing control must be absent natively.");
            return JsonSerializer.Serialize(keys.Select(key => documents[key]).ToArray());
        }
        var before = await ObserveSource();
        var provider = new RavenDBOASIS(new[] { source.Endpoint }, source.Database);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        try
        {
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(
                    CosmicQueryScope.Named(ProviderType.RavenDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                    source.Actor, ProviderType.RavenDBOASIS, catalogue).Result).Result;

            async Task<Holon[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.RavenDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.RavenDBOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: timeout.Token);
                Assert.IsFalse(result.IsError, result.Message);
                return result.Result.Cast<Holon>().ToArray();
            }

            var full = await Read(Query());
            var ordered = source.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(ordered, full.Select(row => row.Id).ToArray());
            var selected = new[] { source.LiveIds[0], source.LiveIds[2] };
            var subset = await Read(Query().Where(row => selected.Contains(row.Id)).Result);
            CollectionAssert.AreEqual(selected.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray(),
                subset.Select(row => row.Id).ToArray());
            var absent = new[] { source.DeletedId, source.MissingId };
            Assert.AreEqual(0, (await Read(Query().Where(row => absent.Contains(row.Id)).Result)).Length);
            var page = await Read(Query().Skip(1).Result.Take(1).Result);
            CollectionAssert.AreEqual(new[] { ordered[1] }, page.Select(row => row.Id).ToArray());
            var again = await Read(Query());
            CollectionAssert.AreEqual(full.Select(row => JsonSerializer.Serialize(row)).ToArray(),
                again.Select(row => JsonSerializer.Serialize(row)).ToArray(), "Read-only fixture changed its source observations.");
            Assert.AreEqual(before, await ObserveSource(), "Native source envelopes changed during read-only qualification.");
        }
        finally
        {
            var stopped = await provider.DeActivateProviderAsync();
            Assert.IsFalse(stopped.IsError, stopped.Message);
        }
    }
}
