using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Weaviate.Client;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class WeaviatePublicProbeNativeTests
{
    private sealed record Catalogue(Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId);

    [TestMethod]
    [TestCategory("WeaviatePublicProbeNative")]
    public async Task PublicProbeActivatesRetainedTlsSourceForExactSdkReads()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_WEAVIATE_CQL_STATE")
            ?? throw new InvalidOperationException("The reviewed local Weaviate environment is required.");
        Assert.IsTrue(OperatingSystem.IsLinux());
        Assert.IsTrue(File.Exists(Path.Combine(state, "environment.json")));
        Assert.AreEqual(Path.Combine(state, "ca.pem"), Environment.GetEnvironmentVariable("SSL_CERT_FILE"));
        var path = Environment.GetEnvironmentVariable("COSMIC_WEAVIATE_PUBLIC_CQL_CATALOGUE")
            ?? throw new InvalidOperationException("A reviewed public teaching catalogue is required.");
        var key = Environment.GetEnvironmentVariable("COSMIC_WEAVIATE_PUBLIC_CQL_API_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "A restricted teaching reader is required.");
        Assert.IsTrue(new FileInfo(path).Length is > 0 and <= 16384);
        var catalogue = JsonSerializer.Deserialize<Catalogue>(await File.ReadAllBytesAsync(path))!;
        Assert.IsNotNull(catalogue);
        Assert.AreNotEqual(Guid.Empty, catalogue.Actor);
        Assert.AreEqual(3, catalogue.LiveIds.Length);
        var ids = catalogue.LiveIds.Append(catalogue.DeletedId).Append(catalogue.MissingId).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));

        using var client = await WeaviateClientBuilder.Local(credentials: Auth.ApiKey(key!),
            hostname: "localhost", restPort: 18080, grpcPort: 15051, useSsl: true)
            .WithoutRetries().WithQueryTimeout(TimeSpan.FromSeconds(15)).BuildAsync();
        using var rest = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var provider = new WeaviateQuerySource(rest, "https://localhost:18080", client, catalogue.LiveIds[0]);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var activated = await provider.ActivateQuerySourceAsync(deadline.Token);
        Assert.IsFalse(activated.IsError, activated.Message);

        const ProviderType authority = ProviderType.WeaviateOASIS;
        async Task<Guid[]> Read(Guid[] selection)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(catalogue.Actor, authority, ids).Result).Result
                .Where(row => selection.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: deadline.Token);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.IsDeleted &&
                row.CreatedByAvatarId == catalogue.Actor && !row.Children.Any()));
            return result.Result.Select(row => row.Id).ToArray();
        }

        var expected = catalogue.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(expected, await Read(ids));
        CollectionAssert.AreEqual(expected.Take(2).ToArray(), await Read(expected.Take(2).ToArray()));
        CollectionAssert.AreEqual(Array.Empty<Guid>(), await Read(new[] { catalogue.DeletedId, catalogue.MissingId }));
    }
}
