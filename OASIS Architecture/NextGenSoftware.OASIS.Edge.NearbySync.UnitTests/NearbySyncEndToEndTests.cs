using FluentAssertions;
using NextGenSoftware.OASIS.Edge.NearbySync;
using Xunit;
using static NextGenSoftware.OASIS.Edge.NearbySync.UnitTests.NearbyTestSupport;

namespace NextGenSoftware.OASIS.Edge.NearbySync.UnitTests;

public class NearbySyncEndToEndTests
{
    private static readonly NearbyNodeOptions Fast = new() { DefaultRequestTimeout = TimeSpan.FromSeconds(3) };

    [Fact]
    public async Task Device_SyncsWithGateway_OverBleSizedFrames()
    {
        var net = new LoopbackNearbyNetwork();
        var deviceLink = net.CreateLink("device", 185);
        var gatewayLink = net.CreateLink("gateway", 185);
        net.Connect(deviceLink, gatewayLink);
        using var deviceId = new NearbyIdentity();
        using var gatewayId = new NearbyIdentity();
        Pair(deviceId, AvatarId, gatewayId, Guid.NewGuid());
        using var device = new NearbyNode(deviceId, new[] { deviceLink }, Fast);
        using var gateway = new NearbyNode(gatewayId, new[] { gatewayLink }, Fast);
        var handler = new EchoExchangeHandler();
        _ = new NearbyPeerResponder(gateway, handler);

        var result = await new NearbyPeerSyncTransport(device, gateway.NodeId).ExchangeAsync(Request(), CancellationToken.None);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.NextPullCheckpoint.Should().Be("cp:start");
        handler.Calls.Should().Be(1);
        deviceLink.FramesSent.Should().BeGreaterThan(1, "a request larger than one BLE frame is chunked");
    }

    [Fact]
    public async Task Responder_RejectsARequestClaimingAnotherAvatar()
    {
        var net = new LoopbackNearbyNetwork();
        var a = net.CreateLink("a"); var b = net.CreateLink("b"); net.Connect(a, b);
        using var idA = new NearbyIdentity(); using var idB = new NearbyIdentity();
        Pair(idA, AvatarId, idB, Guid.NewGuid());
        using var nodeA = new NearbyNode(idA, new[] { a }, Fast); using var nodeB = new NearbyNode(idB, new[] { b }, Fast);
        var handler = new EchoExchangeHandler();
        _ = new NearbyPeerResponder(nodeB, handler);

        var result = await new NearbyPeerSyncTransport(nodeA, nodeB.NodeId).ExchangeAsync(Request(Guid.NewGuid()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("NEARBY_AVATAR_MISMATCH");
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task UntrustedPeer_IsIgnored_AndTimesOut()
    {
        var net = new LoopbackNearbyNetwork();
        var a = net.CreateLink("a"); var b = net.CreateLink("b"); net.Connect(a, b);
        using var idA = new NearbyIdentity(); using var idB = new NearbyIdentity();
        using var nodeA = new NearbyNode(idA, new[] { a }, new NearbyNodeOptions { DefaultRequestTimeout = TimeSpan.FromMilliseconds(400) });
        using var nodeB = new NearbyNode(idB, new[] { b }, Fast);
        var handler = new EchoExchangeHandler();
        _ = new NearbyPeerResponder(nodeB, handler);

        var result = await new NearbyPeerSyncTransport(nodeA, nodeB.NodeId).ExchangeAsync(Request(), CancellationToken.None);

        result.ErrorCode.Should().Be("NEARBY_PEER_TIMEOUT");
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Request_IsRelayedAcrossAMultiHopMesh()
    {
        var net = new LoopbackNearbyNetwork();
        var a = net.CreateLink("a"); var r1 = net.CreateLink("r1"); var r2 = net.CreateLink("r2"); var g = net.CreateLink("g");
        net.Connect(a, r1); net.Connect(r1, r2); net.Connect(r2, g);
        using var idA = new NearbyIdentity(); using var idR1 = new NearbyIdentity(); using var idR2 = new NearbyIdentity(); using var idG = new NearbyIdentity();
        Pair(idA, AvatarId, idG, Guid.NewGuid());
        using var nodeA = new NearbyNode(idA, new[] { a }, Fast);
        using var relay1 = new NearbyNode(idR1, new[] { r1 }, Fast);
        using var relay2 = new NearbyNode(idR2, new[] { r2 }, Fast);
        using var gateway = new NearbyNode(idG, new[] { g }, Fast);
        _ = new NearbyPeerResponder(gateway, new EchoExchangeHandler());

        var result = await new NearbyPeerSyncTransport(nodeA, gateway.NodeId).ExchangeAsync(Request(), CancellationToken.None);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.NextPullCheckpoint.Should().Be("cp:start");
    }

    [Fact]
    public async Task HopLimit_StopsMessagesFromTravellingTooFar()
    {
        var net = new LoopbackNearbyNetwork();
        var a = net.CreateLink("a"); var r1 = net.CreateLink("r1"); var r2 = net.CreateLink("r2"); var g = net.CreateLink("g");
        net.Connect(a, r1); net.Connect(r1, r2); net.Connect(r2, g);
        using var idA = new NearbyIdentity(); using var idR1 = new NearbyIdentity(); using var idR2 = new NearbyIdentity(); using var idG = new NearbyIdentity();
        Pair(idA, AvatarId, idG, Guid.NewGuid());
        var options = new NearbyNodeOptions { MaximumHops = 1, DefaultRequestTimeout = TimeSpan.FromMilliseconds(500) };
        using var nodeA = new NearbyNode(idA, new[] { a }, options);
        using var relay1 = new NearbyNode(idR1, new[] { r1 }, options);
        using var relay2 = new NearbyNode(idR2, new[] { r2 }, options);
        using var gateway = new NearbyNode(idG, new[] { g }, options);
        var handler = new EchoExchangeHandler();
        _ = new NearbyPeerResponder(gateway, handler);

        var result = await new NearbyPeerSyncTransport(nodeA, gateway.NodeId).ExchangeAsync(Request(), CancellationToken.None);

        result.ErrorCode.Should().Be("NEARBY_PEER_TIMEOUT");
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task MeshWithLoops_ProcessesTheRequestOnce()
    {
        var net = new LoopbackNearbyNetwork();
        var a = net.CreateLink("a"); var r1 = net.CreateLink("r1"); var r2 = net.CreateLink("r2"); var g = net.CreateLink("g");
        net.Connect(a, r1); net.Connect(a, r2); net.Connect(r1, g); net.Connect(r2, g); net.Connect(r1, r2);
        using var idA = new NearbyIdentity(); using var idR1 = new NearbyIdentity(); using var idR2 = new NearbyIdentity(); using var idG = new NearbyIdentity();
        Pair(idA, AvatarId, idG, Guid.NewGuid());
        using var nodeA = new NearbyNode(idA, new[] { a }, Fast);
        using var relay1 = new NearbyNode(idR1, new[] { r1 }, Fast);
        using var relay2 = new NearbyNode(idR2, new[] { r2 }, Fast);
        using var gateway = new NearbyNode(idG, new[] { g }, Fast);
        var handler = new EchoExchangeHandler();
        _ = new NearbyPeerResponder(gateway, handler);

        var result = await new NearbyPeerSyncTransport(nodeA, gateway.NodeId).ExchangeAsync(Request(), CancellationToken.None);
        await Task.Delay(200);

        result.IsError.Should().BeFalse(result.Message);
        handler.Calls.Should().Be(1, "duplicate copies arriving over different routes are suppressed");
    }

    [Fact]
    public async Task NoPeersInRange_ReportsNoPeers()
    {
        var net = new LoopbackNearbyNetwork();
        using var id = new NearbyIdentity();
        using var node = new NearbyNode(id, new[] { net.CreateLink("lonely") }, Fast);
        var result = await new NearbyPeerSyncTransport(node, "someone").ExchangeAsync(Request(), CancellationToken.None);
        result.ErrorCode.Should().Be("NEARBY_NO_PEERS");
    }

    [Fact]
    public async Task OverTcp_TwoDevicesSyncOnARealSocket()
    {
        using var serverLink = new TcpNearbyLink(NearbyLinkKind.Lan, 1400);
        using var clientLink = new TcpNearbyLink(NearbyLinkKind.WifiDirect, 1400);
        serverLink.Start(); clientLink.Start();
        await clientLink.ConnectAsync("127.0.0.1", serverLink.Port, CancellationToken.None);
        using var idClient = new NearbyIdentity(); using var idServer = new NearbyIdentity();
        Pair(idClient, AvatarId, idServer, Guid.NewGuid());
        using var client = new NearbyNode(idClient, new[] { clientLink }, Fast);
        using var server = new NearbyNode(idServer, new[] { serverLink }, Fast);
        _ = new NearbyPeerResponder(server, new EchoExchangeHandler());

        var result = await new NearbyPeerSyncTransport(client, server.NodeId).ExchangeAsync(Request(), CancellationToken.None);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.NextPullCheckpoint.Should().Be("cp:start");
    }
}
