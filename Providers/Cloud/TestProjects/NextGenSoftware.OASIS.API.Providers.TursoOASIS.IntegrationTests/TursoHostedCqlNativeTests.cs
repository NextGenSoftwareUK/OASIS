using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS.IntegrationTests;

[TestClass]
public class TursoHostedCqlNativeTests
{
    [TestMethod, TestCategory("TursoHostedCqlNative")]
    public async Task ManagedTeachingDatabaseReturnsOnlyReviewedPublicRows()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_TURSO_HOSTED_CQL_SOURCE");
        var reader = Environment.GetEnvironmentVariable("CQL_TURSO_QUERY_READER_TOKEN");
        if (path == null && reader == null)
        {
            Assert.Inconclusive("Provide a reviewed managed Turso catalogue and protected reader.");
            return;
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(reader),
            "Provide both the reviewed Turso catalogue and protected reader.");

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = deadline.Token;
        var descriptor = await ReadCatalogueAsync(path!, token);
        using var document = JsonDocument.Parse(descriptor, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        CollectionAssert.AreEquivalent(new[] { "Origin", "Actor", "LiveIds", "DeletedId", "MissingId" },
            root.EnumerateObject().Select(property => property.Name).ToArray());
        var origin = new Uri(root.GetProperty("Origin").GetString()!, UriKind.Absolute);
        var actor = CanonicalId(root.GetProperty("Actor"));
        var live = root.GetProperty("LiveIds").EnumerateArray().Select(CanonicalId).ToArray();
        Assert.AreEqual(3, live.Length);
        var inactive = new[] { CanonicalId(root.GetProperty("DeletedId")), CanonicalId(root.GetProperty("MissingId")) };
        var keys = live.Concat(inactive).ToArray();
        Assert.AreEqual(keys.Length, keys.Distinct().Count());

        using var source = new TursoQuerySource(origin, reader!, live[0]);
        var activation = await source.ActivateQuerySourceAsync(token);
        Assert.IsFalse(activation.IsError, activation.Message);
        await QueryAsync(keys, live);
        await QueryAsync(live[..2], live[..2]);
        await QueryAsync(inactive, Array.Empty<Guid>());
        CollectionAssert.AreEqual(descriptor, await ReadCatalogueAsync(path!, token));

        async Task QueryAsync(Guid[] selection, Guid[] expected)
        {
            const ProviderType provider = ProviderType.TursoOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, keys).Result).Result
                .Where(row => selection.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: token);
            Assert.IsFalse(result.IsError, result.Message);
            CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.IsDeleted && row.CreatedByAvatarId == actor));
        }
    }

    private static Guid CanonicalId(JsonElement value)
    {
        var text = value.GetString();
        if (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty || id.ToString("D") != text)
            throw new InvalidDataException("Use canonical nonempty fixture IDs.");
        return id;
    }

    private static async Task<byte[]> ReadCatalogueAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        var buffer = new byte[8193];
        var length = await file.ReadAtLeastAsync(buffer, buffer.Length, false, token);
        if (length == buffer.Length) throw new InvalidDataException("The catalogue exceeds its byte budget.");
        return buffer[..length];
    }
}
