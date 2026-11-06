using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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
public class DenoHostedQueryProtocolTests
{
    private static readonly Guid DatabaseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Uri Endpoint = new("https://api.deno.com/v2/databases/read/v3");
    private const string Key = "oasis/holon/22222222222222222222222222222222";

    [TestMethod]
    public async Task Reviewed_endpoint_receives_only_an_exact_read_with_the_ephemeral_token()
    {
        using var service = new FakeService((request, _) => request.RequestUri.AbsolutePath.EndsWith("/connect")
            ? Metadata(DatabaseId, "../read/v3")
            : ReadResponse(request, "exact"));
        using var backend = Backend(service);

        Assert.AreEqual("record", await backend.GetAsync(Key));
        Assert.AreEqual("record", await backend.GetAsync(Key));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.ListKeysAsync("oasis/"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.PutAsync(Key, "updated"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.DeleteAsync(Key));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.VerifyAsync());
        Assert.AreEqual(3, service.Requests.Count);
        Assert.AreEqual("reader-token", service.Requests[0].Request.Headers.Authorization.Parameter);
        Assert.AreEqual($"https://api.deno.com/v2/databases/{DatabaseId:D}/connect", service.Requests[0].Request.RequestUri.AbsoluteUri);
        using var metadataRequest = JsonDocument.Parse(service.Requests[0].Body);
        CollectionAssert.AreEqual(new[] { 2, 3 }, metadataRequest.RootElement.GetProperty("supportedVersions")
            .EnumerateArray().Select(version => version.GetInt32()).ToArray());
    }

    [TestMethod]
    [DataRow("foreign-origin")]
    [DataRow("wrong-path")]
    [DataRow("wrong-database")]
    public async Task Unreviewed_metadata_never_receives_the_ephemeral_token(string mismatch)
    {
        var databaseId = mismatch == "wrong-database" ? Guid.NewGuid() : DatabaseId;
        var url = mismatch == "foreign-origin" ? "https://unreviewed.example/read/v3" : "../read/v4";
        if (mismatch == "wrong-database") url = Endpoint.AbsoluteUri;
        using var service = new FakeService((_, _) => Metadata(databaseId, url));
        using var backend = Backend(service);

        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => backend.GetAsync(Key));
        Assert.AreEqual(1, service.Requests.Count);
    }

    [TestMethod]
    [DataRow("foreign-key")]
    [DataRow("duplicate")]
    [DataRow("eventual")]
    [DataRow("unspecified")]
    [DataRow("unauthorized")]
    [DataRow("oversized")]
    public async Task Invalid_snapshots_fail_without_replaying_the_read(string variant)
    {
        using var service = new FakeService((request, _) => request.RequestUri.AbsolutePath.EndsWith("/connect")
            ? Metadata(DatabaseId, Endpoint.AbsoluteUri)
            : ReadResponse(request, variant));
        using var backend = Backend(service);

        await Assert.ThrowsExceptionAsync<HttpRequestException>(() => backend.GetAsync(Key));
        Assert.AreEqual(2, service.Requests.Count);
    }

    [TestMethod]
    public async Task Public_exact_id_query_preserves_absence_and_rejects_private_rows_before_paging()
    {
        const ProviderType authority = ProviderType.DenoDeployOASIS;
        const string prefix = "oasis/holon/";
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var privateSecond = false;
        using var service = new FakeService((request, _) =>
        {
            if (request.RequestUri.AbsolutePath.EndsWith("/connect"))
                return Metadata(DatabaseId, Endpoint.AbsoluteUri);

            var range = SnapshotRead.Parser.ParseFrom(request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()).Ranges.Single();
            var nativeKey = DenoKvConnectBackend.DecodeKey(range.Start);
            StringAssert.StartsWith(nativeKey, prefix);
            var id = Guid.ParseExact(nativeKey[prefix.Length..], "N");
            CollectionAssert.Contains(ids, id);
            var rows = new ReadRangeOutput();
            if (id != ids[2])
                rows.Values.Add(new KvEntry
                {
                    Key = range.Start,
                    Value = ByteString.CopyFromUtf8(OasisJson.Serialize(new Holon { Id = id, IsPublic = id != ids[1] || !privateSecond })),
                    Encoding = ValueEncoding.VeBytes
                });
            return Snapshot(rows);
        });
        using var source = new DenoDeployQuerySource(DatabaseId, Endpoint, "reader-token", ids[0], service);
        var activation = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Message);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var targets = new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) };
        var full = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query, authority, targets).Result);
        Assert.IsFalse(full.IsError, full.Message);
        CollectionAssert.AreEquivalent(ids[..2], full.Result.Select(row => row.Id).ToArray());

        privateSecond = true;
        var paged = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query.Take(1).Result, authority, targets).Result);
        Assert.IsTrue(paged.IsError);
        Assert.IsNull(paged.Result);
    }

    private static DenoKvConnectBackend Backend(HttpMessageHandler handler) => new(
        $"https://api.deno.com/v2/databases/{DatabaseId:D}/connect", "reader-token", handler, Endpoint, DatabaseId);

    private static HttpResponseMessage Metadata(Guid databaseId, string url) => FakeService.Json(new
    {
        version = 3,
        uuid = databaseId,
        token = "ephemeral-token",
        expiresAt = DateTimeOffset.UtcNow.AddHours(1),
        endpoints = new[] { new { url, consistency = "strong" } }
    });

    private static HttpResponseMessage ReadResponse(HttpRequestMessage request, string variant)
    {
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual(Endpoint.AbsoluteUri + "/snapshot_read", request.RequestUri.AbsoluteUri);
        Assert.AreEqual("ephemeral-token", request.Headers.Authorization.Parameter);
        Assert.AreEqual("3", request.Headers.GetValues("x-denokv-version").Single());
        Assert.AreEqual(DatabaseId.ToString("D"), request.Headers.GetValues("x-denokv-database-id").Single());
        var range = SnapshotRead.Parser.ParseFrom(request.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()).Ranges.Single();
        var key = DenoKvConnectBackend.EncodeKey(Key);
        CollectionAssert.AreEqual(key, range.Start.ToByteArray());
        CollectionAssert.AreEqual(key.Append((byte)0).ToArray(), range.End.ToByteArray());
        Assert.AreEqual(1, range.Limit);

        if (variant == "unauthorized") return new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var entry = new KvEntry
        {
            Key = ByteString.CopyFrom(variant == "foreign-key" ? DenoKvConnectBackend.EncodeKey("different") : key),
            Value = variant == "oversized" ? ByteString.CopyFrom(new byte[64 * 1024 + 1]) : ByteString.CopyFromUtf8("record"),
            Encoding = ValueEncoding.VeBytes
        };
        var result = new ReadRangeOutput();
        result.Values.Add(entry);
        if (variant == "duplicate") result.Values.Add(entry.Clone());
        return Snapshot(result, variant != "eventual",
            variant == "unspecified" ? SnapshotReadStatus.SrUnspecified : SnapshotReadStatus.SrSuccess);
    }

    private static HttpResponseMessage Snapshot(ReadRangeOutput rows, bool strong = true,
        SnapshotReadStatus status = SnapshotReadStatus.SrSuccess)
    {
        var output = new SnapshotReadOutput { ReadIsStronglyConsistent = strong, Status = status };
        output.Ranges.Add(rows);
        var content = new ByteArrayContent(output.ToByteArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}
