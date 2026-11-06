using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests;

[TestClass]
public class ConvexQuerySourceTests
{
    private static readonly Uri Site = new("https://teaching.example.invalid/");
    private static readonly Guid Probe = Guid.Parse("e9e5bd84-6602-430a-8396-9853da2f20c1");
    private const string Token = "synthetic-convex-teaching-token-123";

    [TestMethod]
    public async Task ActivationUsesOnlyTheExactTeachingRoute()
    {
        using var reply = new Reply(HttpStatusCode.OK, JsonSerializer.Serialize(new { id = Probe, isPublic = true }));
        using var source = new ConvexQuerySource(Site, Token, Probe, reply);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.AreEqual(HttpMethod.Get, reply.Method);
        Assert.AreEqual(new Uri(Site, $"/oasis/cql/holon?id={Probe:D}"), reply.Address);
        Assert.AreEqual("Bearer " + Token, reply.Authorization);
    }

    [DataTestMethod]
    [DataRow(403, "")]
    [DataRow(200, "null")]
    [DataRow(200, "{\"id\":\"51faf2dc-9088-449c-bf52-102637fb5d27\",\"isPublic\":true}")]
    public async Task ActivationFailsForDeniedMissingOrMismatchedRecords(int status, string payload)
    {
        using var reply = new Reply((HttpStatusCode)status, payload);
        using var source = new ConvexQuerySource(Site, Token, Probe, reply);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
    }

    [TestMethod]
    public async Task ExactKeyPlanReturnsPublicRowsAndFailsClosedBeforePaging()
    {
        const ProviderType provider = ProviderType.ConvexOASIS;
        var ids = new[] { Probe, Guid.NewGuid() };
        using var reply = new Reply(HttpStatusCode.OK, JsonSerializer.Serialize(new { id = Probe, isPublic = true }))
        {
            OtherKey = ids[1]
        };
        using var source = new ConvexQuerySource(Site, Token, Probe, reply);
        Assert.IsTrue((await source.ActivateQuerySourceAsync()).Result);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result.Take(1).Result;
        var targets = new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) };
        var plan = CosmicQueryPlanner.Plan(query, provider, targets).Result;

        var result = await source.ExecuteCosmicQueryAsync(plan);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(Probe, result.Result.Single().Id);

        reply.OtherStatus = HttpStatusCode.Forbidden;
        var denied = await source.ExecuteCosmicQueryAsync(plan);
        Assert.IsTrue(denied.IsError);
        Assert.IsNull(denied.Result);
    }

    private sealed class Reply(HttpStatusCode status, string payload) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Address { get; private set; }
        public string? Authorization { get; private set; }
        public Guid? OtherKey { get; init; }
        public HttpStatusCode OtherStatus { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Address = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            var other = OtherKey.HasValue && Address?.Query == $"?id={OtherKey:D}";
            return Task.FromResult(new HttpResponseMessage(other ? OtherStatus : status)
            {
                Content = new StringContent(other ? "null" : payload, Encoding.UTF8, "application/json")
            });
        }
    }
}
