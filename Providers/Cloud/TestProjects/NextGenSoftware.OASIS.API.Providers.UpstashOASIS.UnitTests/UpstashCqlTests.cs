using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests;

[TestClass]
public class UpstashCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.UpstashOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.UpstashOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = UpstashCqlCompiler.Compile(query, ProviderType.UpstashOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(UpstashCqlCompiler.Compile(query, ProviderType.PolygonOASIS).IsError);
        Assert.AreEqual(0, UpstashCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.UpstashOASIS).Result.Count);
    }

    [DataTestMethod]
    [DataRow("name")]
    [DataRow("metadata")]
    [DataRow("order")]
    [DataRow("graph")]
    [DataRow("bound")]
    public void UnsupportedShapesRejectInsteadOfScanningNativeRecords(string shape)
    {
        var query = Query(Enumerable.Range(0, shape == "bound" ? 101 : 3).Select(_ => Guid.NewGuid()).ToArray());
        query = shape switch
        {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };

        var result = UpstashCqlCompiler.Compile(query, ProviderType.UpstashOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new UpstashOASIS("http://127.0.0.1:1", "synthetic-test-token");
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.UpstashOASIS,
            new[] { new CosmicQueryTarget(ProviderType.UpstashOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.AreEqual(typeof(OperationCanceledException), stopped.Exception.GetType());
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public void DerivedAliasesKeepTheirOwnIdentityAndCannotInheritUpstashCertification()
    {
        var provider = new UpstashOASIS("http://127.0.0.1:1", "synthetic-test-token");
        provider.ProviderType.Value = ProviderType.CockroachDBOASIS;
        Assert.AreEqual(ProviderType.CockroachDBOASIS, provider.QueryProviderType);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CockroachDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.CockroachDBOASIS,
                new[] { Guid.NewGuid() }).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.CockroachDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.CockroachDBOASIS, provider.QueryCapabilities) });
        Assert.IsTrue(plan.IsError, "An unadmitted alias cannot expose PostgreSQL query capabilities.");
    }

    [TestMethod]
    public async Task EmptyCatalogueNeedsNoNativeConnectionAndDeactivationClearsAdmission()
    {
        // This endpoint cannot serve SQL. Empty authorized selection must still
        // return an empty page, while provider activation remains mandatory.
        var provider = new UpstashOASIS("http://127.0.0.1:1", "synthetic-test-token")
        { IsProviderActivated = true };
        var plan = CosmicQueryPlanner.Plan(Query(Array.Empty<Guid>()), ProviderType.UpstashOASIS,
            new[] { new CosmicQueryTarget(ProviderType.UpstashOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var empty = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsFalse(empty.IsError, empty.Message);
        Assert.AreEqual(0, empty.Result.Count);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("{\"error\":\"synthetic command rejection\"}", true)]
    [DataRow("{\"result\":\"PONG\"}", false)]
    [DataRow("{\"result\":null}", true)]
    public async Task ActivationUsesCommandOutcomeAndPublishesLifecycle(string body, bool fails)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        server.Start();
        var port = ((System.Net.IPEndPoint)server.LocalEndpoint).Port;
        async Task Respond()
        {
            using var client = await server.AcceptTcpClientAsync(deadline.Token);
            await using var stream = client.GetStream();
            var buffer = new byte[8192];
            await stream.ReadAsync(buffer, deadline.Token);
            var payload = System.Text.Encoding.UTF8.GetBytes(body);
            var header = System.Text.Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + payload.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header, deadline.Token);
            await stream.WriteAsync(payload, deadline.Token);
        }
        var reply = Respond();
        var provider = new UpstashOASIS("http://127.0.0.1:" + port, "synthetic-test-token");
        var result = await provider.ActivateProviderAsync();
        await reply;
        Assert.AreEqual(fails, result.IsError);
        Assert.AreEqual(!fails, provider.IsProviderActivated);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }

}
