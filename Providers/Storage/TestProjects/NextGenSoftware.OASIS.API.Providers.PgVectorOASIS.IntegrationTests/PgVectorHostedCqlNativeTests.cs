using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PgVectorOASIS.IntegrationTests;

[TestClass]
public class PgVectorHostedCqlNativeTests
{
    [TestMethod, TestCategory("PgVectorHostedCqlNative")]
    public async Task HostedPgVectorReturnsOnlyReviewedPublicRows()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_PGVECTOR_HOSTED_CQL_SOURCE");
        var reader = Environment.GetEnvironmentVariable("CQL_PGVECTOR_QUERY_READER_PASSWORD");
        var privateProbe = Environment.GetEnvironmentVariable("COSMIC_PGVECTOR_HOSTED_PRIVATE_ID");
        if (path == null && reader == null && privateProbe == null)
        {
            Assert.Inconclusive("Provide a reviewed PgVector catalogue, private fixture ID and restricted reader.");
            return;
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(reader) || string.IsNullOrWhiteSpace(privateProbe),
            "Provide the reviewed PgVector catalogue, private fixture ID and restricted reader.");

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = deadline.Token;
        var descriptor = await ReadCatalogueAsync(path!, token);
        using var document = JsonDocument.Parse(descriptor, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        CollectionAssert.AreEquivalent(new[] { "Host", "Database", "ReaderUsername", "Actor", "LiveIds", "DeletedId", "MissingId" },
            root.EnumerateObject().Select(property => property.Name).ToArray());
        var host = root.GetProperty("Host").GetString()!;
        var database = root.GetProperty("Database").GetString()!;
        var username = root.GetProperty("ReaderUsername").GetString()!;
        var actor = CanonicalId(root.GetProperty("Actor"));
        var live = root.GetProperty("LiveIds").EnumerateArray().Select(CanonicalId).ToArray();
        Assert.AreEqual(3, live.Length);
        var inactive = new[] { CanonicalId(root.GetProperty("DeletedId")), CanonicalId(root.GetProperty("MissingId")) };
        var keys = live.Concat(inactive).ToArray();
        var deniedId = CanonicalId(privateProbe);
        Assert.AreEqual(keys.Length + 1, keys.Append(deniedId).Distinct().Count());

        var source = new PgVectorQuerySource(host, database, username, reader!, live[0]);
        var activation = await source.ActivateQuerySourceAsync(token);
        Assert.IsFalse(activation.IsError, activation.Message);
        await QueryAsync(keys, live);
        await QueryAsync(live[..2], live[..2]);
        await QueryAsync(inactive, Array.Empty<Guid>());
        await QueryAsync(new[] { live[0], deniedId }, null);
        CollectionAssert.AreEqual(descriptor, await ReadCatalogueAsync(path!, token));

        async Task QueryAsync(Guid[] selection, Guid[]? expected)
        {
            const ProviderType provider = ProviderType.PgVectorOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, selection).Result).Result
                .Where(row => selection.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: token);
            if (expected == null)
            {
                Assert.IsTrue(result.IsError);
                Assert.IsNull(result.Result);
                Assert.AreEqual("A teaching holon differs from the admitted read authority.", result.Exception?.Message);
                return;
            }
            Assert.IsFalse(result.IsError, result.Message);
            CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.IsDeleted && row.CreatedByAvatarId == actor && !row.Children.Any()));
        }
    }

    private static Guid CanonicalId(JsonElement value) => CanonicalId(value.GetString());

    private static Guid CanonicalId(string? text)
    {
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
