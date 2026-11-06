using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS.UnitTests;

[TestClass]
public class TursoNativeCqlTests
{
    [TestMethod, TestCategory("TursoNativeCql")]
    public async Task DisposableLibSqlServerReturnsPublicExactKeyResults()
    {
        var portValue = Environment.GetEnvironmentVariable("COSMIC_TURSO_FIXTURE_PORT");
        if (portValue == null)
        {
            Assert.Inconclusive("Start a dedicated disposable libSQL fixture.");
            return;
        }
        Assert.IsTrue(ushort.TryParse(portValue, out var port) && port >= 1024);
        var local = new Uri($"http://127.0.0.1:{port}/");
        const string token = "synthetic-local-fixture";
        var writer = new TursoOASIS(local.AbsoluteUri, token,
            new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        try
        {
            var ready = await writer.ActivateProviderAsync();
            Assert.IsFalse(ready.IsError, ready.Message);
            var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
            var live = ids[..3];
            var inactive = ids[3..5];
            var actor = Guid.NewGuid();
            foreach (var id in ids.Take(4).Append(ids[5]))
            {
                var saved = await writer.SaveHolonAsync(new Holon
                {
                    Id = id, Name = $"fixture-{id:N}", CreatedByAvatarId = actor, IsPublic = id != ids[5]
                });
                Assert.IsFalse(saved.IsError, saved.Message);
            }
            var deleted = await writer.DeleteHolonAsync(ids[3]);
            Assert.IsFalse(deleted.IsError, deleted.Message);

            using var source = new TursoQuerySource(new Uri("https://teaching.turso.io/"), token, live[0],
                new LoopbackPipeline(local));
            var activation = await source.ActivateQuerySourceAsync();
            Assert.IsFalse(activation.IsError, activation.Exception?.Message);
            await QueryAsync(ids[..5], live);
            await QueryAsync(live[..2], live[..2]);
            await QueryAsync(inactive, Array.Empty<Guid>());
            await QueryAsync(new[] { live[0], ids[5] }, expected: null);

            async Task QueryAsync(Guid[] selection, Guid[]? expected)
            {
                const ProviderType provider = ProviderType.TursoOASIS;
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, ids).Result).Result
                    .Where(row => selection.Contains(row.Id)).Result;
                var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await source.ExecuteCosmicQueryAsync(plan.Result);
                if (expected == null)
                {
                    Assert.IsTrue(result.IsError);
                    Assert.IsNull(result.Result);
                    return;
                }
                Assert.IsFalse(result.IsError, result.Exception?.Message);
                CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
                Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == actor &&
                    row.Name == $"fixture-{row.Id:N}"));
            }
        }
        finally
        {
            writer.DisposeTransport();
        }
    }

    private sealed class LoopbackPipeline(Uri local) :
        DelegatingHandler(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual(new Uri("https://teaching.turso.io/v2/pipeline"), request.RequestUri);
            request.RequestUri = new Uri(local, "/v2/pipeline");
            return base.SendAsync(request, cancellationToken);
        }
    }
}
