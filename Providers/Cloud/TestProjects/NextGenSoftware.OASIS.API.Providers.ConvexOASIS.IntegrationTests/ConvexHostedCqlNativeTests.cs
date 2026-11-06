using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.IntegrationTests;

[TestClass]
public class ConvexHostedCqlNativeTests
{
    [TestMethod, TestCategory("ConvexHostedCqlNative")]
    public async Task DedicatedTeachingSourceReturnsExactPublicRowsWithoutChangingThem()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_CONVEX_HOSTED_CQL_SOURCE");
        var reader = Environment.GetEnvironmentVariable("CQL_CONVEX_QUERY_READER_TOKEN");
        var privateControl = Environment.GetEnvironmentVariable("COSMIC_CONVEX_HOSTED_PRIVATE_ID");
        if (path == null && reader == null && privateControl == null)
        {
            Assert.Inconclusive("Provide a reviewed Convex teaching catalogue and protected reader.");
            return;
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(reader) ||
            string.IsNullOrWhiteSpace(privateControl), "Provide the complete Convex fixture settings.");
        var privateId = CanonicalId(privateControl);

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var token = deadline.Token;
        var descriptor = await ReadDescriptorAsync(path!, token);
        using var document = JsonDocument.Parse(descriptor, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        CollectionAssert.AreEquivalent(new[] { "Site", "Actor", "LiveIds", "DeletedId", "MissingId" },
            root.EnumerateObject().Select(property => property.Name).ToArray());
        var site = new Uri(root.GetProperty("Site").GetString()!, UriKind.Absolute);
        var actor = CanonicalId(root.GetProperty("Actor"));
        var live = root.GetProperty("LiveIds").EnumerateArray().Select(CanonicalId).ToArray();
        Assert.AreEqual(3, live.Length);
        var inactive = new[] { CanonicalId(root.GetProperty("DeletedId")), CanonicalId(root.GetProperty("MissingId")) };
        var keys = live.Concat(inactive).ToArray();
        Assert.AreEqual(keys.Length, keys.Distinct().Count());
        Assert.IsFalse(keys.Contains(privateId));

        using var source = new ConvexQuerySource(site, reader!, live[0]);
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseProxy = false;
        handler.UseCookies = false;
        using var http = new HttpClient(handler);
        using (var anonymous = await http.GetAsync(Route(live[0]), HttpCompletionOption.ResponseHeadersRead, token))
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reader);
        using (var denied = await http.GetAsync(Route(privateId), HttpCompletionOption.ResponseHeadersRead, token))
            Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        var before = await ObserveAsync();
        var activation = await source.ActivateQuerySourceAsync(token);
        Assert.IsFalse(activation.IsError, activation.Message);
        await QueryAsync(keys, live);
        await QueryAsync(live[..2], live[..2]);
        await QueryAsync(inactive, Array.Empty<Guid>());
        CollectionAssert.AreEqual(before, await ObserveAsync());
        CollectionAssert.AreEqual(descriptor, await ReadDescriptorAsync(path!, token));

        Uri Route(Guid id) => new(site, $"/oasis/cql/holon?id={id:D}");

        async Task<string[]> ObserveAsync()
        {
            var hashes = new List<string>();
            foreach (var id in keys)
            {
                using var response = await http.GetAsync(Route(id), HttpCompletionOption.ResponseHeadersRead, token);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                await response.Content.LoadIntoBufferAsync(1024 * 1024, token);
                var payload = await response.Content.ReadAsByteArrayAsync(token);
                using var json = JsonDocument.Parse(payload);
                if (inactive.Contains(id)) Assert.AreEqual(JsonValueKind.Null, json.RootElement.ValueKind);
                else
                {
                    Assert.AreEqual(id, CanonicalId(json.RootElement.GetProperty("id")));
                    Assert.IsTrue(json.RootElement.GetProperty("isPublic").GetBoolean());
                }
                hashes.Add(Convert.ToHexString(SHA256.HashData(payload)));
            }
            return hashes.ToArray();
        }

        async Task QueryAsync(Guid[] selection, Guid[] expected)
        {
            const ProviderType provider = ProviderType.ConvexOASIS;
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

    private static Guid CanonicalId(JsonElement value) => CanonicalId(value.GetString());

    private static Guid CanonicalId(string? text)
    {
        if (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty || id.ToString("D") != text)
            throw new InvalidDataException("Use canonical nonempty fixture IDs.");
        return id;
    }

    private static async Task<byte[]> ReadDescriptorAsync(string path, CancellationToken token)
    {
        await using var file = File.OpenRead(path);
        var buffer = new byte[8193];
        var length = await file.ReadAtLeastAsync(buffer, buffer.Length, false, token);
        if (length == buffer.Length) throw new InvalidDataException("The descriptor exceeds its byte budget.");
        return buffer[..length];
    }
}
