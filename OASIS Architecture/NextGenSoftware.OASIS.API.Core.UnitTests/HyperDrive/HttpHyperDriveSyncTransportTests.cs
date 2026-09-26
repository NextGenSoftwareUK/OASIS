using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HttpHyperDriveSyncTransportTests
{
    [Fact]
    public async Task SendsCanonicalRequestAndReadsOasisResult()
    {
        var expected = new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse { NextPullCheckpoint = "next" });
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = JsonContent(expected) });
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://onode.test/") };
        var transport = new HttpHyperDriveSyncTransport(client);
        var request = new SyncExchangeRequest { AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid() };

        var result = await transport.ExchangeAsync(request, default);

        result.Result.NextPullCheckpoint.Should().Be("next");
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://onode.test/api/hyperdrive/sync/exchange"));
        handler.LastBody.Should().Contain(request.DeviceId.ToString());
    }

    [Fact]
    public async Task PreservesStructuredServerError()
    {
        var error = new OASISResult<SyncExchangeResponse> { IsError = true, ErrorCode = "SYNC_DENIED", Message = "Denied" };
        var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        { Content = JsonContent(error) })) { BaseAddress = new Uri("https://onode.test/") };
        var result = await new HttpHyperDriveSyncTransport(client).ExchangeAsync(new SyncExchangeRequest(), default);
        result.ErrorCode.Should().Be("SYNC_DENIED");
        result.Message.Should().Be("Denied");
    }

    [Fact]
    public async Task ReadsAspNetStringEnumResponse()
    {
        const string response = """
            {"isError":false,"result":{"protocolVersion":3,"operationResults":[{"operationId":"00000000-0000-0000-0000-000000000001","disposition":"Accepted","resultVersionId":"00000000-0000-0000-0000-000000000002"}],"remoteChanges":[],"hasMoreRemoteChanges":false}}
            """;
        var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(response, Encoding.UTF8, "application/json") }))
        { BaseAddress = new Uri("https://onode.test/") };

        var result = await new HttpHyperDriveSyncTransport(client).ExchangeAsync(new SyncExchangeRequest(), default);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.OperationResults.Single().Disposition.Should().Be(SyncOperationDisposition.Accepted);
    }

    [Fact]
    public async Task NonJsonServerFailureReportsStatusAndResponseBody()
    {
        var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        { Content = new StringContent("upstream application error") })) { BaseAddress = new Uri("https://onode.test/") };

        var result = await new HttpHyperDriveSyncTransport(client).ExchangeAsync(new SyncExchangeRequest(), default);

        result.ErrorCode.Should().Be("HYPERDRIVE_HTTP_INVALID_JSON");
        result.Message.Should().Contain("HTTP 500").And.Contain("upstream application error");
    }

    [Fact]
    public async Task NetworkFailureLeavesExplicitOfflineSafeError()
    {
        var client = new HttpClient(new Handler(_ => throw new HttpRequestException("offline")))
        { BaseAddress = new Uri("https://onode.test/") };
        var result = await new HttpHyperDriveSyncTransport(client).ExchangeAsync(new SyncExchangeRequest(), default);
        result.ErrorCode.Should().Be("HYPERDRIVE_NETWORK_UNAVAILABLE");
        result.Message.Should().Contain("remain queued");
    }

    [Fact]
    public async Task HttpClientTimeoutIsClassifiedAsConnectivityFailureButCallerCancellationPropagates()
    {
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(new AsyncHandler((_, _) =>
            throw new TaskCanceledException("timeout"))) { BaseAddress = new Uri("https://onode.test/") });

        var timeout = await transport.ExchangeAsync(new SyncExchangeRequest(), default);
        timeout.ErrorCode.Should().Be("HYPERDRIVE_NETWORK_TIMEOUT");
        timeout.Message.Should().Contain("remain queued");

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await FluentActions.Awaiting(() => transport.ExchangeAsync(new SyncExchangeRequest(), cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task TransientGatewayResponsesEnterOfflineSafeClassification(HttpStatusCode statusCode)
    {
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(new Handler(_ =>
            new HttpResponseMessage(statusCode))) { BaseAddress = new Uri("https://onode.test/") });

        var result = await transport.ExchangeAsync(new SyncExchangeRequest(), default);

        result.ErrorCode.Should().Be("HYPERDRIVE_REMOTE_UNAVAILABLE");
    }

    [Fact]
    public async Task BindPeerSendsCanonicalRequestToAuthenticatedBindingEndpoint()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = JsonContent(new OASISResult<bool> { Result = true }) });
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(handler)
        { BaseAddress = new Uri("https://onode.test/") });
        var request = new BindHyperDrivePeerRequest
        { DeviceId = Guid.NewGuid(), NodeId = "node", PublicKey = "key", Signature = "proof" };

        var result = await transport.BindPeerAsync(request, default);

        result.IsError.Should().BeFalse(result.Message);
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://onode.test/api/hyperdrive/sync/onet-peer-binding"));
        handler.LastBody.Should().Contain(request.DeviceId.ToString()).And.Contain("proof");
    }

    [Fact]
    public async Task BindPeerPreservesBindingConflictFromServer()
    {
        var error = new OASISResult<bool>
        { IsError = true, ErrorCode = "MONGO_PEER_ALREADY_BOUND", Message = "already bound" };
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(new Handler(_ =>
            new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent(error) }))
        { BaseAddress = new Uri("https://onode.test/") });

        var result = await transport.BindPeerAsync(new BindHyperDrivePeerRequest(), default);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("MONGO_PEER_ALREADY_BOUND");
    }

    [Fact]
    public async Task RequestsOfflineGrantFromAuthenticatedCanonicalEndpoint()
    {
        var grant = new HyperDriveOfflineSessionGrant { GrantId = "grant", AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid() };
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = JsonContent(new OASISResult<HyperDriveOfflineSessionGrant>(grant)) });
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(handler)
        { BaseAddress = new Uri("https://onode.test/") });

        var result = await transport.IssueOfflineSessionGrantAsync(new IssueHyperDriveOfflineSessionGrantRequest
        { DeviceId = grant.DeviceId, RequestedLifetimeMinutes = 60, RequestedScopes = new[] { "world.read" } }, default);

        result.Result.GrantId.Should().Be("grant");
        handler.LastRequest.RequestUri.Should().Be(new Uri("https://onode.test/api/hyperdrive/sync/offline-session-grant"));
        handler.LastBody.Should().Contain("world.read").And.Contain(grant.DeviceId.ToString());
    }

    [Fact]
    public async Task OfflineGrantScopeRejectionIsPreserved()
    {
        var error = new OASISResult<HyperDriveOfflineSessionGrant>
        { IsError = true, ErrorCode = "OFFLINE_GRANT_SCOPE_FORBIDDEN", Message = "forbidden" };
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(new Handler(_ =>
            new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = JsonContent(error) }))
        { BaseAddress = new Uri("https://onode.test/") });

        var result = await transport.IssueOfflineSessionGrantAsync(new IssueHyperDriveOfflineSessionGrantRequest(), default);
        result.ErrorCode.Should().Be("OFFLINE_GRANT_SCOPE_FORBIDDEN");
    }

    [Fact]
    public async Task SignedOfflineGrantIsAttachedOnlyToSyncExchangePayload()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = JsonContent(new OASISResult<SyncExchangeResponse>(new SyncExchangeResponse())) });
        var transport = new HttpHyperDriveSyncTransport(new HttpClient(handler)
        { BaseAddress = new Uri("https://onode.test/") });
        var grant = new HyperDriveOfflineSessionGrant
        { GrantId = "offline", AvatarId = Guid.NewGuid(), DeviceId = Guid.NewGuid(), Signature = "proof" };
        transport.SetOfflineSessionGrant(grant);

        await transport.ExchangeAsync(new SyncExchangeRequest
        { AvatarId = grant.AvatarId, DeviceId = grant.DeviceId }, default);

        handler.LastBody.Should().Contain("offline").And.Contain("proof");
    }

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private sealed class Handler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handle;
        public HttpRequestMessage LastRequest { get; private set; }
        public string LastBody { get; private set; }
        public Handler(Func<HttpRequestMessage, HttpResponseMessage> handle) => _handle = handle;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync();
            return _handle(request);
        }
    }


    private sealed class AsyncHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handle;
        public AsyncHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handle) => _handle = handle;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _handle(request, cancellationToken);
    }
}
