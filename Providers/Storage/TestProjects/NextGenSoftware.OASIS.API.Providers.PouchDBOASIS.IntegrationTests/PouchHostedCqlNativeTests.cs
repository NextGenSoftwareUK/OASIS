using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests;

[TestClass]
public class PouchHostedCqlNativeTests
{
    private sealed record Source(string Endpoint, string Digest, Guid Actor, Guid[] LiveIds, Guid MissingId,
        string OriginalProofPath, string OriginalProofSha256, string SourceAckPath, string SourceAckSha256);

    [TestMethod]
    [TestCategory("PouchHostedCqlNative")]
    public async Task OwnedReadOnlyRuntimeSupportsNativeCqlAndPreservesOriginalWireRecords()
    {
        var descriptor = Environment.GetEnvironmentVariable("COSMIC_POUCHDB_HOSTED_CQL_SOURCE")
            ?? throw new InvalidOperationException("Explicit retained hosted PouchDB source descriptor required.");
        Assert.IsTrue(new FileInfo(descriptor).Length is > 0 and <= 16384);
        var descriptorBytes = await File.ReadAllBytesAsync(descriptor);
        var source = JsonSerializer.Deserialize<Source>(descriptorBytes, new JsonSerializerOptions {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })!;
        Assert.IsNotNull(source);
        Assert.AreEqual("http://127.0.0.1:13800", source.Endpoint);
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.IsNotNull(source.LiveIds); Assert.AreEqual(3, source.LiveIds.Length);
        var identities = source.LiveIds.Append(source.MissingId).ToArray();
        Assert.AreEqual(4, identities.Distinct().Count()); Assert.IsFalse(identities.Contains(Guid.Empty));
        static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var proofBytes = await File.ReadAllBytesAsync(source.OriginalProofPath);
        Assert.AreEqual(source.OriginalProofSha256, Hash(proofBytes));
        Assert.AreEqual(source.SourceAckSha256, Hash(await File.ReadAllBytesAsync(source.SourceAckPath)));
        using var proof = JsonDocument.Parse(proofBytes);
        Assert.AreEqual(source.Digest, proof.RootElement.GetProperty("digest").GetString());
        var original = proof.RootElement.GetProperty("records").EnumerateArray().ToDictionary(
            row => row.GetProperty("id").GetGuid(),
            row => (Revision: row.GetProperty("revision").GetString(), WireSha256: row.GetProperty("httpSha256").GetString()));
        CollectionAssert.AreEquivalent(source.LiveIds, original.Keys.ToArray());

        using var http = new HttpClient(new CqlPouchDbSourceHttpHandler(source.Digest,
            new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false })) {
            Timeout = TimeSpan.FromSeconds(15), MaxResponseContentBufferSize = 1024 * 1024 };
        var provider = new PouchDBOASIS(source.Endpoint, null, null, http);
        async Task<string[]> Observe()
        {
            var result = new List<string>();
            foreach (var id in source.LiveIds)
            {
                using var response = await http.GetAsync(source.Endpoint + $"/oasis_holons/{id:D}?conflicts=true");
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                var bytes = await response.Content.ReadAsByteArrayAsync();
                Assert.AreEqual(original[id].WireSha256, Hash(bytes));
                using var row = JsonDocument.Parse(bytes);
                Assert.AreEqual(id, row.RootElement.GetProperty("_id").GetGuid());
                Assert.AreEqual(id, row.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(source.Actor, row.RootElement.GetProperty("CreatedByAvatarId").GetGuid());
                Assert.AreEqual(original[id].Revision, row.RootElement.GetProperty("_rev").GetString());
                Assert.IsTrue(row.RootElement.GetProperty("IsPublic").GetBoolean());
                result.Add(Hash(bytes));
            }
            using var absent = await http.GetAsync(source.Endpoint + $"/oasis_holons/{source.MissingId:D}?conflicts=true");
            Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
            using var missing = JsonDocument.Parse(await absent.Content.ReadAsByteArrayAsync());
            Assert.AreEqual("not_found", missing.RootElement.GetProperty("error").GetString());
            Assert.AreEqual("missing", missing.RootElement.GetProperty("reason").GetString());
            return result.ToArray();
        }
        try
        {
            var before = await Observe();
            var active = await provider.ActivateQuerySourceAsync(default);
            Assert.IsFalse(active.IsError, active.Exception?.ToString() ?? active.Message);
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PouchDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, ProviderType.PouchDBOASIS, source.LiveIds).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.PouchDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.PouchDBOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var read = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(read.IsError, read.Exception?.ToString() ?? read.Message);
                Assert.IsTrue(read.Result.All(row => row.CreatedByAvatarId == source.Actor && row.IsPublic && !row.IsDeleted && !row.Children.Any()));
                return read.Result.Select(row => row.Id).ToArray();
            }
            var ordered = source.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(ordered, await Read(Query()));
            var subset = new[] { ordered[0], ordered[2] };
            CollectionAssert.AreEqual(subset, await Read(Query().Where(row => subset.Contains(row.Id)).Result));
            CollectionAssert.AreEqual(new[] { ordered[1] }, await Read(Query().Skip(1).Result.Take(1).Result));
            var missingId = source.MissingId;
            var missingQuery = Query().Where(row => row.Id == missingId);
            Assert.IsFalse(missingQuery.IsError, missingQuery.Message);
            Assert.AreEqual(0, (await Read(missingQuery.Result)).Length);
            CollectionAssert.AreEqual(before, await Observe());
            Assert.AreEqual(source.SourceAckSha256, Hash(await File.ReadAllBytesAsync(source.SourceAckPath)));
            CollectionAssert.AreEqual(descriptorBytes, await File.ReadAllBytesAsync(descriptor));
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
