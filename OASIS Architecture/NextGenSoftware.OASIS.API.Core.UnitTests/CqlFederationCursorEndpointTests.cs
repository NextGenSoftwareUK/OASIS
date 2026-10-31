using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static (DefaultHttpContext Http, PreparedStorageExecution Policy, List<UsageProviderReceipt> Receipts)
        CursorAdmission(CqlFederationCursorHost host, Guid key, Guid? admittedKey = null)
    {
        var basis = Admission(FederationProviders[0]);
        var handle = new UsageExecutionHandle(new UsageOutboxOperation
        {
            Id = UsageCanonical.OperationId("WEB4", Actor.ToString("D"), (admittedKey ?? key).ToString("D")),
            Service = "WEB4", UserId = Actor.ToString("D"), ExecutionOwner = Guid.NewGuid().ToString("D"),
            Policy = new UsageEndpointPolicy { RequiresProviderMeasurement = true }
        }, basis.Store.Object, new Web4UsageLedgerOptions { Service = "WEB4" }, new(), Mock.Of<IUsageReservationAuthority>());
        basis.Http.Items[Web4UsageMeasurement.ExecutionKey] = handle;
        basis.Http.Request.Headers["Idempotency-Key"] = key.ToString("D");
        basis.Http.RequestServices = new ServiceCollection().AddSingleton(host).BuildServiceProvider();
        return (basis.Http, new PreparedStorageExecution(handle, "cursor-http-test", new Dictionary<ProviderType, string>(),
            FederationProviders, FederationProviders.Select(type => new StorageQueryTariff(type, 0.002m, "test-v1"))), basis.Receipts);
    }

    private static CqlFederationV1Request CursorRequest(string? token = null) => new()
    {
        EnableContinuation = true, Cursor = token, Scope = "AllEligible", Take = 1,
        MaximumRowsPerProvider = 10, MaximumInputRows = 30, MaximumProviderAttempts = 12
    };

    private static FederationSources CursorSources()
    {
        var sources = new FederationSources();
        for (var index = 0; index < FederationProviders.Length; index++)
            sources.Items[FederationProviders[index]].Rows.Add(new Holon
            { Id = Guid.NewGuid(), Name = ((char)('A' + index)).ToString(), CreatedByAvatarId = Actor });
        return sources;
    }

    [Fact]
    public async Task OptInRequiresExplicitHostKeyEnrollmentBeforeProviderLookup()
    {
        var admitted = FederationAdmission();
        using var services = new ServiceCollection().BuildServiceProvider();
        admitted.Http.RequestServices = services;
        var sources = CursorSources();
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(
            admitted.Http, CursorRequest(), sources));
        Assert.Equal("CQL_CURSOR_UNAVAILABLE", Code(result));
        sources.NeverRead();
        Assert.All(sources.Items.Values, source => Assert.Equal(0, source.Lookups));
        Assert.Empty(admitted.Receipts);
    }

    [Fact]
    public async Task DepletedProtectedReadSetRetainsNoDispatchEvidence()
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('1', 64));
        var first = CursorAdmission(host, Guid.NewGuid());
        var response = Assert.IsType<FileContentResult>(await first.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(first.Http, CursorRequest(), CursorSources())));
        using var json = JsonDocument.Parse(response.FileContents);
        var continuation = json.RootElement.GetProperty("result").GetProperty("continuation");
        var state = host.Read(continuation.GetProperty("cursor").GetString()!, Actor).Result;
        var depleted = new CqlFederationCursorState(state.Actor, state.Intent, state.Deployment,
            state.Lineage, state.NextRequestKey, state.Issued, state.Expires, state.Offset,
            state.Providers.ToArray(), state.InputRows, state.ProviderRows.ToArray(), state.InputBytes,
            state.OutputBytes, state.Work, 0, state.DurationMilliseconds);
        var next = CursorAdmission(host, state.NextRequestKey);
        var sources = CursorSources();
        var result = await next.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(next.Http,
            CursorRequest(host.Codec.Protect(depleted).Result), sources));
        Assert.Equal("CQL_FEDERATION_ATTEMPT_LIMIT", Code(result));
        sources.NeverRead();
        Assert.Empty(next.Receipts);
        Assert.False(next.Http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FreshPolicyDenialOrCatalogueChangeRejectsContinuationBeforeDispatch(bool denied)
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('d', 64));
        var sources = CursorSources();
        var first = CursorAdmission(host, Guid.NewGuid());
        var response = Assert.IsType<FileContentResult>(await first.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(first.Http, CursorRequest(), sources)));
        using var json = JsonDocument.Parse(response.FileContents);
        var continuation = json.RootElement.GetProperty("result").GetProperty("continuation");
        var next = CursorAdmission(host, Guid.Parse(continuation.GetProperty("nextRequestKey").GetString()!));
        var policy = new FederationPolicy { Denied = denied ? ProviderType.SQLLiteDBOASIS : null };
        using var services = new ServiceCollection().AddSingleton(host).AddSingleton<ICqlReadAccessPolicy>(policy).BuildServiceProvider();
        next.Http.RequestServices = services;
        var fresh = CursorSources();
        // The catalogue capability is preflighted before any physical read.
        foreach (var source in fresh.Items.Values)
            source.Adapter.SetupGet(value => value.QueryCapabilities).Returns(new CosmicQueryCapabilities(
                Capabilities.Features | CosmicQueryFeatures.Membership, Capabilities.CanonicalFields.ToArray(),
                Array.Empty<CosmicQueryMetadataCapability>()));
        var result = await next.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(next.Http,
            CursorRequest(continuation.GetProperty("cursor").GetString()), fresh));
        Assert.IsType<ObjectResult>(result);
        fresh.NeverRead();
        Assert.Empty(next.Receipts);
        Assert.False(next.Http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey));
    }

    [Fact]
    public async Task ContinuationPinsTheOriginalDefaultDespiteLaterDefaultChange()
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('e', 64));
        var sources = CursorSources();
        sources.Items[ProviderType.MongoDBOASIS].Rows.Add(new Holon
            { Id = Guid.NewGuid(), Name = "B", CreatedByAvatarId = Actor });
        var first = CursorAdmission(host, Guid.NewGuid());
        var request = CursorRequest();
        request.Scope = "CurrentDefault";
        var response = Assert.IsType<FileContentResult>(await first.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(first.Http, request, sources)));
        using var json = JsonDocument.Parse(response.FileContents);
        var continuation = json.RootElement.GetProperty("result").GetProperty("continuation");
        sources.Default = ProviderType.Neo4jOASIS;
        var next = CursorAdmission(host, Guid.Parse(continuation.GetProperty("nextRequestKey").GetString()!));
        request.Cursor = continuation.GetProperty("cursor").GetString();
        var second = Assert.IsType<FileContentResult>(await next.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(next.Http, request, sources)));
        using var output = JsonDocument.Parse(second.FileContents);
        Assert.Equal("B", output.RootElement.GetProperty("result").GetProperty("rows")[0].GetProperty("selected").GetProperty("row").GetProperty("name").GetString());
        Assert.Single(next.Receipts);
        Assert.Equal(2, sources.Items[ProviderType.MongoDBOASIS].Lookups);
        Assert.Equal(0, sources.Items[ProviderType.Neo4jOASIS].Lookups);
    }

    [Fact]
    public async Task ConcurrentRedemptionReusesAtomicDurableClaimAndRejectsTheSecondOwner()
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('f', 64));
        var first = CursorAdmission(host, Guid.NewGuid());
        var response = Assert.IsType<FileContentResult>(await first.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(first.Http, CursorRequest(), CursorSources())));
        using var json = JsonDocument.Parse(response.FileContents);
        var continuation = json.RootElement.GetProperty("result").GetProperty("continuation");
        var key = continuation.GetProperty("nextRequestKey").GetString()!;
        var store = new Mock<IUsageOutboxStore>();
        UsageOutboxOperation? claimed = null;
        store.Setup(value => value.TryBeginAsync(It.IsAny<UsageOutboxOperation>(), It.IsAny<CancellationToken>()))
            .Returns((UsageOutboxOperation operation, CancellationToken _) =>
                Task.FromResult(Interlocked.CompareExchange(ref claimed, operation, null) == null));
        store.Setup(value => value.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, CancellationToken _) => Task.FromResult(claimed));
        var authority = new Mock<IUsageReservationAuthority>();
        authority.Setup(value => value.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .Returns((string _, UsageAuthorizationRequest request, CancellationToken _) => Task.FromResult(new SubscriptionAuthorizationResult
            { Allowed = true, UserId = Actor.ToString("D"), OperationId = request.OperationId, Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) }));
        authority.Setup(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()))
            .Returns((UsageStartRequest request, CancellationToken _) => Task.FromResult(new UsageStartResult
            { Started = true, OperationId = request.OperationId }));
        var execution = new Web4UsageExecution(authority.Object, store.Object, new Web4UsageLedgerOptions { Service = "WEB4" });
        async Task<UsageExecutionHandle?> Redeem()
        {
            try
            {
                return await execution.BeginAsync("test-bearer", Actor.ToString("D"), key, new string('a', 64),
                    "POST /api/data/query-cql/federation", new UsageEndpointPolicy
                    { MeterCategory = "storage-query", MaximumProviderAttempts = 3, RequiresProviderMeasurement = true });
            }
            catch (UsageProtocolException cause) { Assert.Equal("OPERATION_ALREADY_EXECUTED", cause.Code); return null; }
        }
        var admitted = await Task.WhenAll(Task.Run(Redeem), Task.Run(Redeem));
        var winner = Assert.Single(admitted.Where(value => value != null))!;
        var http = new DefaultHttpContext();
        http.Request.Headers["Idempotency-Key"] = key;
        http.Items[Web4UsageMeasurement.ExecutionKey] = winner;
        var state = host.Read(continuation.GetProperty("cursor").GetString()!, Actor).Result;
        Assert.False(host.BindRequest(http, Actor, state).IsError);
        authority.Verify(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HttpCursorAdvancesGlobalPageWithPinnedScopeAndCumulativeAccounting()
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('a', 64));
        var sources = CursorSources();
        string? token = null;
        var key = Guid.NewGuid();
        CqlFederationCursorState? previous = null;
        for (var page = 0; page < 3; page++)
        {
            var admitted = CursorAdmission(host, key);
            var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(
                admitted.Http, CursorRequest(token), sources));
            var file = Assert.IsType<FileContentResult>(result);
            using var json = JsonDocument.Parse(file.FileContents);
            var output = json.RootElement.GetProperty("result");
            Assert.Equal(((char)('A' + page)).ToString(), output.GetProperty("rows")[0].GetProperty("selected").GetProperty("row").GetProperty("name").GetString());
            Assert.Equal(3, admitted.Receipts.Count);
            Assert.Equal(0.006m, admitted.Receipts.Sum(receipt => receipt.ActualCostUsd));
            Assert.Equal(page < 2, output.GetProperty("hasMore").GetBoolean());
            var continuation = output.GetProperty("continuation");
            if (page == 2) { Assert.Equal(JsonValueKind.Null, continuation.ValueKind); break; }
            token = continuation.GetProperty("cursor").GetString();
            key = Guid.Parse(continuation.GetProperty("nextRequestKey").GetString()!);
            var decoded = host.Read(token!, Actor);
            Assert.False(decoded.IsError, decoded.Message);
            Assert.Equal(page + 1, decoded.Result.Offset);
            Assert.Equal(12 - (page + 1) * 3, decoded.Result.Attempts);
            Assert.Equal(FederationProviders, decoded.Result.Providers);
            if (previous != null)
            {
                Assert.Equal(previous.Lineage, decoded.Result.Lineage);
                Assert.Equal(previous.Expires, decoded.Result.Expires);
                Assert.True(decoded.Result.InputBytes < previous.InputBytes);
                Assert.True(decoded.Result.OutputBytes < previous.OutputBytes);
                Assert.True(decoded.Result.Work < previous.Work);
                Assert.True(decoded.Result.DurationMilliseconds < previous.DurationMilliseconds);
            }
            previous = decoded.Result;
        }
    }

    [Theory]
    [InlineData("wrong-key")]
    [InlineData("wrong-admission")]
    [InlineData("intent")]
    [InlineData("tamper")]
    public async Task RejectedContinuationNeverDispatchesOrCreatesReceipt(string mode)
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('b', 64));
        var sources = CursorSources();
        var first = CursorAdmission(host, Guid.NewGuid());
        var response = Assert.IsType<FileContentResult>(await first.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(first.Http, CursorRequest(), sources)));
        using var json = JsonDocument.Parse(response.FileContents);
        var continuation = json.RootElement.GetProperty("result").GetProperty("continuation");
        var token = continuation.GetProperty("cursor").GetString()!;
        var key = Guid.Parse(continuation.GetProperty("nextRequestKey").GetString()!);
        var fresh = CursorSources();
        var next = CursorAdmission(host, mode == "wrong-key" ? Guid.NewGuid() : key,
            mode == "wrong-admission" ? Guid.NewGuid() : null);
        var request = CursorRequest(mode == "tamper" ? "not-a-protected-cursor" : token);
        if (mode == "intent") request.Take = 2;
        var result = await next.Policy.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(next.Http, request, fresh));
        Assert.IsType<ObjectResult>(result);
        fresh.NeverRead();
        Assert.Empty(next.Receipts);
        Assert.False(next.Http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey));
    }

    [Fact]
    public async Task MoreRowsWithExhaustedChainBudgetAreExplicitAndHaveNoUsableCursor()
    {
        var host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('c', 64));
        var admitted = CursorAdmission(host, Guid.NewGuid());
        var request = CursorRequest();
        request.MaximumProviderAttempts = 3;
        var response = Assert.IsType<FileContentResult>(await admitted.Policy.RunAsync(() =>
            CqlV1Endpoint.ExecuteFederationAsync(admitted.Http, request, CursorSources())));
        using var json = JsonDocument.Parse(response.FileContents);
        var output = json.RootElement.GetProperty("result");
        Assert.True(output.GetProperty("hasMore").GetBoolean());
        Assert.Equal(JsonValueKind.Null, output.GetProperty("continuation").ValueKind);
        Assert.Equal("BudgetExhausted", output.GetProperty("continuationUnavailable").GetString());
    }
}
