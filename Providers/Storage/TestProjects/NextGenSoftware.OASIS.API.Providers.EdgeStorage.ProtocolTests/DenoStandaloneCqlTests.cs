using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS;
using NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS.Datapath;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class DenoStandaloneCqlTests
{
    [TestMethod]
    public async Task Native_standalone_snapshots_return_exact_CQL_rows_without_changing_versionstamps()
    {
        var token = Environment.GetEnvironmentVariable("COSMIC_DENO_KV_NATIVE_TOKEN");
        if (string.IsNullOrWhiteSpace(token)) Assert.Inconclusive("Requires the isolated denokv fixture.");
        var origin = new Uri("http://127.0.0.1:54865/");
        var endpoint = new Uri(origin, "v2");
        using var handler = new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false, UseCookies = false };
        using var writer = new DenoKvConnectBackend(origin.AbsoluteUri, token, handler);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = deadline.Token;
        var actor = Guid.NewGuid();
        var live = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var tombstone = Guid.NewGuid();
        var absent = Guid.NewGuid();
        var ids = live.Append(tombstone).Append(absent).ToArray();
        foreach (var id in ids[..4])
        {
            var row = new Holon { Id = id, IsPublic = true, CreatedByAvatarId = actor,
                DeletedDate = id == tombstone ? DateTime.UtcNow : DateTime.MinValue };
            await writer.PutAsync(Key(id), OasisJson.Serialize(row), ct);
        }

        using var http = new HttpClient(handler, disposeHandler: false);
        using var metadataRequest = new HttpRequestMessage(HttpMethod.Post, origin)
        {
            Content = JsonContent.Create(new { supportedVersions = new[] { 2, 3 } })
        };
        metadataRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var metadataResponse = await http.SendAsync(metadataRequest, ct);
        metadataResponse.EnsureSuccessStatusCode();
        using var metadata = JsonDocument.Parse(await metadataResponse.Content.ReadAsByteArrayAsync(ct));
        Assert.AreEqual(3, metadata.RootElement.GetProperty("version").GetInt32());
        Assert.AreEqual(Guid.Empty, metadata.RootElement.GetProperty("databaseId").GetGuid());
        var nativeToken = metadata.RootElement.GetProperty("token").GetString();

        var before = await SnapshotAsync();
        Assert.IsTrue(before[..4].All(entry => entry != null && entry.Versionstamp.Length == 10));
        Assert.IsNull(before[4]);
        using var source = new DenoDeployQuerySource(new DenoKvConnectBackend(origin.AbsoluteUri, token, handler, endpoint, Guid.Empty), live[0]);
        var activation = await source.ActivateQuerySourceAsync(ct);
        Assert.IsFalse(activation.IsError, activation.Exception?.GetType().Name);
        await CheckQueryAsync(ids, live);
        await CheckQueryAsync(live[..2], live[..2]);
        await CheckQueryAsync(new[] { tombstone, absent }, Array.Empty<Guid>());
        CollectionAssert.AreEqual(before, await SnapshotAsync());

        static string Key(Guid id) => $"oasis/holon/{id:N}";

        async Task<KvEntry[]> SnapshotAsync()
        {
            var read = new SnapshotRead();
            foreach (var id in ids)
            {
                var key = DenoKvConnectBackend.EncodeKey(Key(id));
                read.Ranges.Add(new ReadRange { Start = ByteString.CopyFrom(key),
                    End = ByteString.CopyFrom(key.Append((byte)0).ToArray()), Limit = 1 });
            }
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(origin, "v2/snapshot_read"))
            {
                Content = new ByteArrayContent(read.ToByteArray())
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", nativeToken);
            request.Headers.Add("x-denokv-version", "3");
            request.Headers.Add("x-denokv-database-id", Guid.Empty.ToString("D"));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var output = SnapshotReadOutput.Parser.ParseFrom(await response.Content.ReadAsByteArrayAsync(ct));
            Assert.IsTrue(output.ReadIsStronglyConsistent);
            Assert.AreEqual(SnapshotReadStatus.SrSuccess, output.Status);
            Assert.AreEqual(ids.Length, output.Ranges.Count);
            return output.Ranges.Select(range => range.Values.SingleOrDefault()).ToArray();
        }

        async Task CheckQueryAsync(Guid[] selection, Guid[] expected)
        {
            const ProviderType provider = ProviderType.DenoDeployOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, ids).Result).Result
                .Where(row => selection.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: ct);
            Assert.IsFalse(result.IsError, result.Message);
            CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.IsDeleted && row.CreatedByAvatarId == actor));
        }
    }
}
