using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.UnitTests;

[TestClass]
public class MinioCqlTests
{
    private sealed class Client(Func<Guid, GetObjectResponse> read) : AmazonS3Client(new AnonymousAWSCredentials(), new AmazonS3Config { ServiceURL = "http://127.0.0.1:1", ForcePathStyle = true })
    {
        public List<Guid> ReadIds = new();
        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual("teaching", request.BucketName);
            StringAssert.StartsWith(request.Key, "oasis-holons/");
            StringAssert.EndsWith(request.Key, ".json");
            var id = Guid.Parse(request.Key[13..^5]); ReadIds.Add(id);
            return Task.FromResult(read(id));
        }
    }
    private static GetObjectResponse Response(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return new GetObjectResponse { HttpStatusCode = HttpStatusCode.OK, ContentLength = bytes.Length, ResponseStream = new MemoryStream(bytes) };
    }
    private static string Payload(Guid id, bool deleted = false) => JsonSerializer.Serialize(new Holon {
        Id = id, Name = "Native object", DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue,
        Children = new[] { new Holon { Id = Guid.NewGuid(), Name = "Embedded child" } } });
    private static AmazonS3Exception NativeError(string code, HttpStatusCode status) => new("Controlled native failure") { ErrorCode = code, StatusCode = status };
    private static CosmicQuery<Holon> Query(Guid[] ids) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MinIOOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MinIOOASIS, ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(MinIOOASIS provider, CosmicQuery<Holon> query) => CosmicQueryPlanner.Plan(query,
        ProviderType.MinIOOASIS, new[] { new CosmicQueryTarget(ProviderType.MinIOOASIS, provider.QueryCapabilities) }).Result;
    private static MinIOOASIS Provider(Guid probe, Client client) => new("http://127.0.0.1:1", "controlled", "controlled", "teaching", false, probe, client);

    [TestMethod]
    public async Task NativeKeysReturnThreeTwoExcludedAndPageWithEmbeddedChildrenProjectedOut()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        using var client = new Client(id => id == ids[4] ? throw NativeError("NoSuchKey", HttpStatusCode.NotFound) : Response(Payload(id, id == ids[3])));
        var provider = Provider(ids[0], client);
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        var all = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsFalse(all.IsError, all.Exception?.ToString() ?? all.Message);
        CollectionAssert.AreEqual(ids.Take(3).ToArray(), all.Result.Select(row => row.Id).ToArray());
        Assert.IsTrue(all.Result.All(row => !row.Children.Any()));
        var subset = new[] { ids[0], ids[2] };
        var filtered = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids).Where(row => subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError, filtered.Message); CollectionAssert.AreEqual(subset, filtered.Result.Select(row => row.Id).ToArray());
        var page = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids).Skip(1).Result.Take(1).Result));
        Assert.IsFalse(page.IsError, page.Message); Assert.AreEqual(ids[1], page.Result.Single().Id);
    }

    [DataTestMethod]
    [DataRow("NoSuchBucket")]
    [DataRow("AccessDenied")]
    [DataRow("ServerError")]
    [DataRow("wrong-id")]
    [DataRow("duplicate")]
    [DataRow("oversized")]
    [DataRow("truncated")]
    public async Task NativeFailuresCannotBecomeMissingOrPartialRows(string fault)
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        GetObjectResponse Truncated(Guid id) { var result = Response(Payload(id)); result.ContentLength++; return result; }
        using var client = new Client(id => id == ids[0] ? Response(Payload(id)) : fault switch {
            "NoSuchBucket" => throw NativeError(fault, HttpStatusCode.NotFound),
            "AccessDenied" => throw NativeError(fault, HttpStatusCode.Forbidden),
            "ServerError" => throw NativeError(fault, HttpStatusCode.InternalServerError),
            "wrong-id" => Response(Payload(Guid.NewGuid())),
            "duplicate" => Response("{\"Id\":\"" + id + "\",\"Id\":\"" + id + "\"}"),
            "truncated" => Truncated(id),
            _ => Response(new string('x', 1024 * 1024 + 1)) });
        var provider = Provider(ids[0], client); provider.IsProviderActivated = true;
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(fault == "oversized" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
        if (fault is "NoSuchBucket" or "AccessDenied" or "ServerError") Assert.IsInstanceOfType<AmazonS3Exception>(result.Exception);
        if (fault is "wrong-id" or "duplicate" or "truncated") Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
    }

    [TestMethod]
    public async Task FailedReadOnlyProbeClearsAdmissionAndDoesNotProvision()
    {
        var id = Guid.NewGuid();
        using var client = new Client(_ => throw NativeError("NoSuchBucket", HttpStatusCode.NotFound));
        var provider = Provider(id, client); provider.IsProviderActivated = true;
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(provider.IsProviderActivated); CollectionAssert.AreEqual(new[] { id }, client.ReadIds);
    }

    [TestMethod]
    public async Task CallerSelectionsCannotExpandTrustedReadCatalogue()
    {
        var allowed = Guid.NewGuid(); var foreign = Guid.NewGuid();
        using var client = new Client(id => Response(Payload(id)));
        var provider = Provider(allowed, client);
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        client.ReadIds.Clear();
        var selected = new[] { allowed, foreign };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(new[] { allowed }).Where(row => selected.Contains(row.Id)).Result));
        Assert.IsFalse(result.IsError, result.Message); Assert.AreEqual(allowed, result.Result.Single().Id);
        CollectionAssert.AreEqual(new[] { allowed }, client.ReadIds);
        await provider.DeActivateProviderAsync();
        var inactive = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(new[] { allowed })));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
    }

    [TestMethod]
    public async Task CancelledProbeDoesNotReadOrRemainAdmitted()
    {
        var id = Guid.NewGuid(); using var client = new Client(key => Response(Payload(key)));
        var provider = Provider(id, client); provider.IsProviderActivated = true;
        var result = await provider.ActivateQuerySourceAsync(new CancellationToken(true));
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(0, client.ReadIds.Count);
    }
}
